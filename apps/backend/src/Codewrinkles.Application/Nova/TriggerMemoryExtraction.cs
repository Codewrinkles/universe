using System.Text;
using System.Text.Json;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

public sealed record TriggerMemoryExtractionCommand(
    Guid ProfileId
) : ICommand<TriggerMemoryExtractionResult>;

public sealed record TriggerMemoryExtractionResult(
    int SessionsProcessed,
    int TotalMemoriesCreated
);

public sealed class TriggerMemoryExtractionCommandHandler
    : ICommandHandler<TriggerMemoryExtractionCommand, TriggerMemoryExtractionResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILlmService _llmService;
    private readonly IEmbeddingService _embeddingService;

    public TriggerMemoryExtractionCommandHandler(
        IUnitOfWork unitOfWork,
        ILlmService llmService,
        IEmbeddingService embeddingService)
    {
        _unitOfWork = unitOfWork;
        _llmService = llmService;
        _embeddingService = embeddingService;
    }

    public async Task<TriggerMemoryExtractionResult> HandleAsync(
        TriggerMemoryExtractionCommand command,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Nova.TriggerMemoryExtraction);
        activity?.SetProfileId(command.ProfileId);

        try
        {
            // Get all sessions that need memory extraction
            var sessions = await _unitOfWork.Nova.GetSessionsNeedingMemoryExtractionAsync(
                command.ProfileId,
                cancellationToken);

            if (sessions.Count == 0)
            {
                activity?.SetSuccess(true);
                return new TriggerMemoryExtractionResult(0, 0);
            }

            var totalMemoriesCreated = 0;
            var sessionsProcessed = 0;

            foreach (var session in sessions)
            {
                var memoriesCreated = await ProcessSessionAsync(
                    command.ProfileId,
                    session,
                    cancellationToken);

                totalMemoriesCreated += memoriesCreated;
                sessionsProcessed++;
            }

            activity?.SetSuccess(true);
            activity?.SetTag("sessions.processed", sessionsProcessed);
            activity?.SetTag("memories.created", totalMemoriesCreated);

            return new TriggerMemoryExtractionResult(sessionsProcessed, totalMemoriesCreated);
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }

    private async Task<int> ProcessSessionAsync(
        Guid profileId,
        ConversationSession session,
        CancellationToken cancellationToken)
    {
        // Get messages after the last processed message
        var messages = await _unitOfWork.Nova.GetMessagesAfterAsync(
            session.Id,
            session.LastProcessedMessageId,
            cancellationToken);

        if (messages.Count == 0)
        {
            return 0;
        }

        // Build transcript for LLM
        var transcript = BuildTranscript(messages);

        // Call LLM to extract memories
        var extractedMemories = await ExtractMemoriesFromTranscriptAsync(
            transcript,
            cancellationToken);

        if (extractedMemories.Count == 0)
        {
            // Mark extraction as complete even if no memories found
            // Need to re-fetch session for tracking since we got it with AsNoTracking
            var trackedSession = await _unitOfWork.Nova.FindSessionByIdAsync(
                session.Id,
                cancellationToken);

            if (trackedSession is not null)
            {
                var lastMessageId = messages[^1].Id;
                trackedSession.MarkMemoryExtracted(lastMessageId);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return 0;
        }

        // Process extracted memories
        var memoriesCreated = 0;

        foreach (var extracted in extractedMemories)
        {
            // Generate embedding for the memory
            var embedding = await _embeddingService.GetEmbeddingAsync(
                extracted.Content,
                cancellationToken);
            var embeddingBytes = _embeddingService.SerializeEmbedding(embedding);

            if (Memory.IsSingleCardinality(extracted.Category))
            {
                // Single cardinality - supersede existing if present
                var existing = await _unitOfWork.NovaMemories.FindActiveByCategoryAsync(
                    profileId,
                    extracted.Category,
                    cancellationToken);

                var newMemory = Memory.Create(
                    profileId,
                    session.Id,
                    extracted.Category,
                    extracted.Content,
                    embeddingBytes,
                    extracted.Importance);

                _unitOfWork.NovaMemories.Create(newMemory);
                memoriesCreated++;

                if (existing is not null)
                {
                    existing.Supersede(newMemory.Id);
                    _unitOfWork.NovaMemories.Update(existing);
                }
            }
            else
            {
                // Multi cardinality - check for exact content match
                var existing = await _unitOfWork.NovaMemories.FindByContentAsync(
                    profileId,
                    extracted.Category,
                    extracted.Content,
                    cancellationToken);

                if (existing is not null)
                {
                    // Reinforce existing memory
                    existing.IncrementOccurrence();
                    if (extracted.Importance > existing.Importance)
                    {
                        existing.UpdateImportance(extracted.Importance);
                    }
                    _unitOfWork.NovaMemories.Update(existing);
                }
                else
                {
                    // Create new memory
                    var newMemory = Memory.Create(
                        profileId,
                        session.Id,
                        extracted.Category,
                        extracted.Content,
                        embeddingBytes,
                        extracted.Importance);

                    _unitOfWork.NovaMemories.Create(newMemory);
                    memoriesCreated++;
                }
            }
        }

        // Mark extraction as complete - re-fetch for tracking
        var sessionToUpdate = await _unitOfWork.Nova.FindSessionByIdAsync(
            session.Id,
            cancellationToken);

        if (sessionToUpdate is not null)
        {
            var lastProcessedMessageId = messages[^1].Id;
            sessionToUpdate.MarkMemoryExtracted(lastProcessedMessageId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return memoriesCreated;
    }

    private static string BuildTranscript(IReadOnlyList<Message> messages)
    {
        var sb = new StringBuilder();
        foreach (var msg in messages)
        {
            var role = msg.Role == MessageRole.User ? "User" : "Nova";
            sb.AppendLine($"{role}: {msg.Content}");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private async Task<List<ExtractedMemory>> ExtractMemoriesFromTranscriptAsync(
        string transcript,
        CancellationToken cancellationToken)
    {
        var prompt = BuildExtractionPrompt(transcript);

        var messages = new List<LlmMessage>
        {
            new(MessageRole.System, "You are a memory extraction assistant. Extract key memories from conversations and return them as JSON. Be concise and specific."),
            new(MessageRole.User, prompt)
        };

        var response = await _llmService.GetChatCompletionAsync(messages, cancellationToken);

        return ParseExtractionResponse(response.Content);
    }

    private static string BuildExtractionPrompt(string transcript)
    {
        return $$"""
            Analyze this conversation between a learner and an AI coach named Nova.
            Extract key memories about the learner that would be useful for future conversations.

            Conversation:
            {{transcript}}

            Return a JSON object with these fields (all arrays can be empty):
            {
              "topics_discussed": ["topic1", "topic2"],
              "concepts_explained": ["concept1", "concept2"],
              "struggles_identified": ["struggle1"],
              "strengths_demonstrated": ["strength1"],
              "current_focus": "what they're working on" or null,
              "importance_notes": {
                "topic1": 4,
                "struggle1": 5
              }
            }

            Guidelines:
            - Be specific and concise (each item should be 1-2 sentences max)
            - Only include things actually discussed, don't infer
            - Rate importance 1-5 (5 = critical to remember)
            - current_focus should capture their main project/learning goal if mentioned
            - Return ONLY valid JSON, no markdown code blocks
            """;
    }

    private static List<ExtractedMemory> ParseExtractionResponse(string content)
    {
        var memories = new List<ExtractedMemory>();

        try
        {
            // Clean up response - remove markdown code blocks if present
            var json = content.Trim();
            if (json.StartsWith("```"))
            {
                var lines = json.Split('\n');
                var startIndex = 1;
                var endIndex = lines.Length - 1;
                if (lines[^1].Trim() == "```")
                {
                    endIndex = lines.Length - 2;
                }
                json = string.Join('\n', lines[startIndex..(endIndex + 1)]);
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Parse topics_discussed
            if (root.TryGetProperty("topics_discussed", out var topics))
            {
                foreach (var topic in topics.EnumerateArray())
                {
                    var text = topic.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var importance = GetImportance(root, text);
                        memories.Add(new ExtractedMemory(
                            MemoryCategory.TopicDiscussed,
                            text,
                            importance));
                    }
                }
            }

            // Parse concepts_explained
            if (root.TryGetProperty("concepts_explained", out var concepts))
            {
                foreach (var concept in concepts.EnumerateArray())
                {
                    var text = concept.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var importance = GetImportance(root, text);
                        memories.Add(new ExtractedMemory(
                            MemoryCategory.ConceptExplained,
                            text,
                            importance));
                    }
                }
            }

            // Parse struggles_identified
            if (root.TryGetProperty("struggles_identified", out var struggles))
            {
                foreach (var struggle in struggles.EnumerateArray())
                {
                    var text = struggle.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var importance = GetImportance(root, text);
                        memories.Add(new ExtractedMemory(
                            MemoryCategory.StruggleIdentified,
                            text,
                            importance));
                    }
                }
            }

            // Parse strengths_demonstrated
            if (root.TryGetProperty("strengths_demonstrated", out var strengths))
            {
                foreach (var strength in strengths.EnumerateArray())
                {
                    var text = strength.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var importance = GetImportance(root, text);
                        memories.Add(new ExtractedMemory(
                            MemoryCategory.StrengthDemonstrated,
                            text,
                            importance));
                    }
                }
            }

            // Parse current_focus (single cardinality)
            if (root.TryGetProperty("current_focus", out var focus) &&
                focus.ValueKind == JsonValueKind.String)
            {
                var text = focus.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var importance = GetImportance(root, text);
                    memories.Add(new ExtractedMemory(
                        MemoryCategory.CurrentFocus,
                        text,
                        Math.Max(importance, 4)));
                }
            }
        }
        catch (JsonException)
        {
            // Return empty list if parsing fails
        }

        return memories;
    }

    private static int GetImportance(JsonElement root, string key)
    {
        if (root.TryGetProperty("importance_notes", out var notes) &&
            notes.TryGetProperty(key, out var importance) &&
            importance.ValueKind == JsonValueKind.Number)
        {
            var value = importance.GetInt32();
            return Math.Clamp(value, Memory.MinImportance, Memory.MaxImportance);
        }

        return Memory.DefaultImportance;
    }

    private sealed record ExtractedMemory(
        MemoryCategory Category,
        string Content,
        int Importance);
}
