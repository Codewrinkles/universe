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

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILlmService _llmService;

    public SendMessageCommandHandler(
        IUnitOfWork unitOfWork,
        ILlmService llmService)
    {
        _unitOfWork = unitOfWork;
        _llmService = llmService;
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

            // Create user message
            var userMessage = Message.CreateUserMessage(session.Id, command.Message);
            _unitOfWork.Nova.CreateMessage(userMessage);

            // Get conversation history for context
            var history = await _unitOfWork.Nova.GetMessagesBySessionIdAsync(
                session.Id,
                limit: MaxContextMessages,
                cancellationToken: cancellationToken);

            // Build messages for LLM (system prompt + history + new message)
            var llmMessages = BuildLlmMessages(history, command.Message, learnerProfile);

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
        LearnerProfile? learnerProfile)
    {
        // Build personalized system prompt
        var systemPrompt = SystemPrompts.BuildPersonalizedPrompt(learnerProfile);

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
