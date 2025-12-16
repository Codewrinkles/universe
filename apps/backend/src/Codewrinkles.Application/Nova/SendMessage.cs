using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

public sealed record SendMessageCommand(
    Guid ProfileId,
    string Message,
    Guid? SessionId = null
) : ICommand<SendMessageResult>;

public sealed record SendMessageResult(
    Guid SessionId,
    Guid MessageId,
    string Response,
    DateTimeOffset CreatedAt,
    bool IsNewSession
);

public sealed class SendMessageCommandHandler
    : ICommandHandler<SendMessageCommand, SendMessageResult>
{
    /// <summary>
    /// Maximum number of messages to include in conversation context.
    /// Prevents token overflow for long conversations.
    /// </summary>
    private const int MaxContextMessages = 20;

    private const int RecentMemoriesCount = 5;
    private const int HighImportanceThreshold = 4;
    private const int HighImportanceLimit = 5;
    private const int SemanticSearchLimit = 10;
    private const int MaxTotalMemories = 20;
    private const float MinSemanticSimilarity = 0.7f;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILlmService _llmService;
    private readonly IEmbeddingService _embeddingService;

    public SendMessageCommandHandler(
        IUnitOfWork unitOfWork,
        ILlmService llmService,
        IEmbeddingService embeddingService)
    {
        _unitOfWork = unitOfWork;
        _llmService = llmService;
        _embeddingService = embeddingService;
    }

    public async Task<SendMessageResult> HandleAsync(
        SendMessageCommand command,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Nova.SendMessage);
        activity?.SetProfileId(command.ProfileId);

        try
        {
            // Fetch learner profile for personalization
            var learnerProfile = await _unitOfWork.Nova.FindLearnerProfileByProfileIdAsync(
                command.ProfileId,
                cancellationToken);

            var isNewSession = false;
            ConversationSession session;

            // Get or create session
            if (command.SessionId.HasValue)
            {
                var existingSession = await _unitOfWork.Nova.FindSessionByIdAsync(
                    command.SessionId.Value,
                    cancellationToken);

                if (existingSession is null || existingSession.IsDeleted)
                {
                    throw new Domain.Nova.Exceptions.ConversationNotFoundException(command.SessionId.Value);
                }

                if (existingSession.ProfileId != command.ProfileId)
                {
                    throw new Domain.Nova.Exceptions.ConversationAccessDeniedException(
                        command.SessionId.Value,
                        command.ProfileId);
                }

                session = existingSession;
            }
            else
            {
                // Create new session
                session = ConversationSession.Create(command.ProfileId);
                _unitOfWork.Nova.CreateSession(session);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                isNewSession = true;
            }

            activity?.SetEntity("ConversationSession", session.Id);

            // Get relevant memories for context
            var memories = await GetMemoriesForContextAsync(
                command.ProfileId,
                command.Message,
                cancellationToken);

            // Create user message
            var userMessage = Message.CreateUserMessage(session.Id, command.Message);
            _unitOfWork.Nova.CreateMessage(userMessage);

            // Get conversation history for context
            var history = await _unitOfWork.Nova.GetMessagesBySessionIdAsync(
                session.Id,
                limit: MaxContextMessages,
                cancellationToken: cancellationToken);

            // Build messages for LLM (system prompt + history + new message)
            var llmMessages = BuildLlmMessages(history, command.Message, learnerProfile, memories);

            // Get AI response
            var llmResponse = await _llmService.GetChatCompletionAsync(llmMessages, cancellationToken);

            // Create assistant message
            var assistantMessage = Message.CreateAssistantMessage(
                session.Id,
                llmResponse.Content,
                tokensUsed: llmResponse.InputTokens + llmResponse.OutputTokens,
                modelUsed: llmResponse.ModelUsed);

            _unitOfWork.Nova.CreateMessage(assistantMessage);

            // Update session
            session.UpdateLastMessageAt();

            // Generate title for new sessions based on first message
            if (isNewSession)
            {
                var title = GenerateTitle(command.Message);
                session.UpdateTitle(title);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Record metrics
            AppMetrics.RecordNovaMessage(
                isNewSession,
                llmResponse.InputTokens,
                llmResponse.OutputTokens);

            activity?.SetSuccess(true);

            return new SendMessageResult(
                SessionId: session.Id,
                MessageId: assistantMessage.Id,
                Response: llmResponse.Content,
                CreatedAt: assistantMessage.CreatedAt,
                IsNewSession: isNewSession);
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }

    private static List<LlmMessage> BuildLlmMessages(
        IReadOnlyList<Message> history,
        string newMessage,
        LearnerProfile? learnerProfile,
        IReadOnlyList<MemoryDto> memories)
    {
        // Build personalized system prompt with memories
        var systemPrompt = SystemPrompts.BuildPersonalizedPrompt(learnerProfile, memories);

        var messages = new List<LlmMessage>
        {
            // System prompt first (personalized if profile exists)
            new(MessageRole.System, systemPrompt)
        };

        // Add conversation history
        foreach (var msg in history)
        {
            messages.Add(new LlmMessage(msg.Role, msg.Content));
        }

        // Add the new user message
        messages.Add(new LlmMessage(MessageRole.User, newMessage));

        return messages;
    }

    private async Task<IReadOnlyList<MemoryDto>> GetMemoriesForContextAsync(
        Guid profileId,
        string currentMessage,
        CancellationToken cancellationToken)
    {
        // Get recent memories
        var recentMemories = await _unitOfWork.NovaMemories.GetRecentAsync(
            profileId,
            RecentMemoriesCount,
            cancellationToken);

        // Get high-importance memories
        var highImportanceMemories = await _unitOfWork.NovaMemories.GetByMinImportanceAsync(
            profileId,
            HighImportanceThreshold,
            HighImportanceLimit,
            cancellationToken);

        // Get semantically relevant memories
        var semanticMemories = await GetSemanticallySimilarMemoriesAsync(
            profileId,
            currentMessage,
            cancellationToken);

        // Merge and deduplicate
        return MergeAndDeduplicateMemories(
            recentMemories,
            highImportanceMemories,
            semanticMemories);
    }

    private async Task<List<(Memory Memory, float Similarity)>> GetSemanticallySimilarMemoriesAsync(
        Guid profileId,
        string currentMessage,
        CancellationToken cancellationToken)
    {
        // Get all memories with embeddings
        var memoriesWithEmbeddings = await _unitOfWork.NovaMemories.GetWithEmbeddingsAsync(
            profileId,
            cancellationToken);

        if (memoriesWithEmbeddings.Count == 0)
        {
            return [];
        }

        // Generate embedding for current message
        var messageEmbedding = await _embeddingService.GetEmbeddingAsync(
            currentMessage,
            cancellationToken);

        // Calculate similarity for each memory
        var scored = new List<(Memory Memory, float Similarity)>();

        foreach (var memory in memoriesWithEmbeddings)
        {
            if (memory.Embedding is null)
            {
                continue;
            }

            var memoryEmbedding = _embeddingService.DeserializeEmbedding(memory.Embedding);
            var similarity = _embeddingService.CosineSimilarity(messageEmbedding, memoryEmbedding);

            if (similarity >= MinSemanticSimilarity)
            {
                scored.Add((memory, similarity));
            }
        }

        // Return top N by similarity
        return scored
            .OrderByDescending(x => x.Similarity)
            .Take(SemanticSearchLimit)
            .ToList();
    }

    private static IReadOnlyList<MemoryDto> MergeAndDeduplicateMemories(
        IReadOnlyList<Memory> recentMemories,
        IReadOnlyList<Memory> highImportanceMemories,
        List<(Memory Memory, float Similarity)> semanticMemories)
    {
        var seen = new HashSet<Guid>();
        var result = new List<(Memory Memory, float Score)>();

        // Add semantic memories first (highest priority)
        foreach (var (memory, similarity) in semanticMemories.OrderByDescending(x => x.Similarity))
        {
            if (seen.Add(memory.Id))
            {
                result.Add((memory, Score: similarity + 1)); // Boost for semantic match
            }
        }

        // Add high importance memories
        foreach (var memory in highImportanceMemories.OrderByDescending(m => m.Importance))
        {
            if (seen.Add(memory.Id))
            {
                result.Add((memory, Score: memory.Importance / 5.0f));
            }
        }

        // Add recent memories
        for (var i = 0; i < recentMemories.Count; i++)
        {
            var memory = recentMemories[i];
            if (seen.Add(memory.Id))
            {
                // Score decreases by position (most recent = highest score)
                result.Add((memory, Score: (recentMemories.Count - i) / (float)recentMemories.Count * 0.5f));
            }
        }

        // Sort by score descending, limit, and convert to DTOs
        return result
            .OrderByDescending(m => m.Score)
            .Take(MaxTotalMemories)
            .Select(m => new MemoryDto(
                m.Memory.Id,
                m.Memory.Category.ToString(),
                m.Memory.Content,
                m.Memory.Importance,
                m.Memory.CreatedAt))
            .ToList();
    }

    private static string GenerateTitle(string firstMessage)
    {
        // Simple title generation - take first ~50 chars of message
        // In a production system, you might use the LLM to generate better titles
        var title = firstMessage.Trim();

        if (title.Length <= 50)
        {
            return title;
        }

        // Find a good break point
        var breakPoint = title.LastIndexOf(' ', 50);
        if (breakPoint < 20)
        {
            breakPoint = 50;
        }

        return title[..breakPoint].TrimEnd() + "...";
    }
}
