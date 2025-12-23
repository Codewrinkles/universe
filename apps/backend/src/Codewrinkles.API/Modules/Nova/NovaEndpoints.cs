using System.Text;
using System.Text.Json;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Application.Nova;
using Codewrinkles.Application.Nova.Services;
using Codewrinkles.API.Extensions;
using Codewrinkles.Domain.Nova;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Codewrinkles.API.Modules.Nova;

public static class NovaEndpoints
{
    public static void MapNovaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/nova")
            .WithTags("Nova")
            .RequireAuthorization("RequiresNovaAccess"); // Requires authentication + Nova access

        group.MapPost("chat", SendMessage)
            .WithName("NovaSendMessage");

        group.MapPost("chat/stream", SendMessageStream)
            .WithName("NovaSendMessageStream");

        group.MapGet("sessions", GetConversations)
            .WithName("NovaGetConversations");

        group.MapGet("sessions/{sessionId:guid}", GetConversation)
            .WithName("NovaGetConversation");

        group.MapDelete("sessions/{sessionId:guid}", DeleteConversation)
            .WithName("NovaDeleteConversation");

        // Learner Profile endpoints
        group.MapGet("profile", GetLearnerProfile)
            .WithName("NovaGetLearnerProfile");

        group.MapPut("profile", UpdateLearnerProfile)
            .WithName("NovaUpdateLearnerProfile");

        // Memory extraction endpoint (for testing/manual trigger)
        group.MapPost("memories/extract", TriggerMemoryExtraction)
            .WithName("NovaTriggerMemoryExtraction");
    }

    private static async Task<IResult> SendMessage(
        HttpContext httpContext,
        [FromBody] SendMessageRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var profileId = httpContext.GetCurrentProfileId();

        var command = new SendMessageCommand(
            ProfileId: profileId,
            Message: request.Message,
            SessionId: request.SessionId);

        var result = await mediator.SendAsync(command, cancellationToken);

        return Results.Ok(new
        {
            sessionId = result.SessionId,
            messageId = result.MessageId,
            response = result.Response,
            createdAt = result.CreatedAt,
            isNewSession = result.IsNewSession
        });
    }

    private static async Task SendMessageStream(
        HttpContext httpContext,
        [FromBody] SendMessageRequest request,
        [FromServices] IUnitOfWork unitOfWork,
        [FromServices] ILlmService llmService,
        [FromServices] IEmbeddingService embeddingService,
        [FromServices] IEnumerable<INovaPlugin> novaPlugins,
        [FromServices] IMediator mediator,
        [FromServices] IMemoryExtractionQueue memoryExtractionQueue,
        CancellationToken cancellationToken)
    {
        const int MaxContextMessages = 20;

        var profileId = httpContext.GetCurrentProfileId();
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Nova.SendMessage);
        activity?.SetProfileId(profileId);

        // Fetch learner profile for personalization
        var learnerProfile = await unitOfWork.Nova.FindLearnerProfileByProfileIdAsync(
            profileId,
            cancellationToken);

        var isNewSession = false;
        ConversationSession session;

        // Get or create session
        if (request.SessionId.HasValue)
        {
            var existingSession = await unitOfWork.Nova.FindSessionByIdAsync(
                request.SessionId.Value,
                cancellationToken);

            if (existingSession is null || existingSession.IsDeleted)
            {
                // Set SSE headers before sending error
                httpContext.Response.ContentType = "text/event-stream";
                httpContext.Response.Headers.CacheControl = "no-cache";
                httpContext.Response.Headers.Connection = "keep-alive";
                await WriteSSEError(httpContext, "Conversation not found");
                return;
            }

            if (existingSession.ProfileId != profileId)
            {
                httpContext.Response.ContentType = "text/event-stream";
                httpContext.Response.Headers.CacheControl = "no-cache";
                httpContext.Response.Headers.Connection = "keep-alive";
                await WriteSSEError(httpContext, "Access denied");
                return;
            }

            session = existingSession;
        }
        else
        {
            // NEW CONVERSATION: Queue memory extraction from previous sessions
            // This runs in background so the first message responds immediately
            // Extracted memories will be available for subsequent sessions
            await memoryExtractionQueue.QueueExtractionAsync(profileId, cancellationToken);

            session = ConversationSession.Create(profileId);
            unitOfWork.Nova.CreateSession(session);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            isNewSession = true;
        }

        // Get relevant memories for context (after extraction, so new memories are available)
        var memories = await GetMemoriesForStreamingContextAsync(
            unitOfWork,
            embeddingService,
            profileId,
            request.Message,
            cancellationToken);

        // Set SSE headers
        httpContext.Response.ContentType = "text/event-stream";
        httpContext.Response.Headers.CacheControl = "no-cache";
        httpContext.Response.Headers.Connection = "keep-alive";

        activity?.SetEntity("ConversationSession", session.Id);

        // Create user message
        var userMessage = Message.CreateUserMessage(session.Id, request.Message);
        unitOfWork.Nova.CreateMessage(userMessage);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Get conversation history for context
        var history = await unitOfWork.Nova.GetMessagesBySessionIdAsync(
            session.Id,
            limit: MaxContextMessages,
            cancellationToken: cancellationToken);

        // Build messages for LLM with personalized system prompt and memories
        var systemPrompt = SystemPrompts.BuildPersonalizedPrompt(learnerProfile, memories);
        var llmMessages = new List<LlmMessage>
        {
            new(MessageRole.System, systemPrompt)
        };

        foreach (var msg in history)
        {
            llmMessages.Add(new LlmMessage(msg.Role, msg.Content));
        }

        llmMessages.Add(new LlmMessage(MessageRole.User, request.Message));

        // Send initial metadata
        await WriteSSEData(httpContext, new
        {
            type = "start",
            sessionId = session.Id,
            isNewSession
        });

        // Stream the response
        var fullResponse = new StringBuilder();
        var inputTokens = 0;
        var outputTokens = 0;
        var modelUsed = string.Empty;

        // Convert plugins to object list for LLM service
        var plugins = novaPlugins.Cast<object>().ToList();

        try
        {
            await foreach (var chunk in llmService.GetStreamingChatCompletionWithToolsAsync(llmMessages, plugins, cancellationToken))
            {
                if (chunk.IsComplete)
                {
                    inputTokens = chunk.InputTokens ?? 0;
                    outputTokens = chunk.OutputTokens ?? 0;
                    modelUsed = chunk.ModelUsed ?? string.Empty;
                }
                else
                {
                    fullResponse.Append(chunk.Content);
                    await WriteSSEData(httpContext, new
                    {
                        type = "content",
                        content = chunk.Content
                    });
                }
            }

            // Save assistant message
            var assistantMessage = Message.CreateAssistantMessage(
                session.Id,
                fullResponse.ToString(),
                tokensUsed: inputTokens + outputTokens,
                modelUsed: modelUsed);

            unitOfWork.Nova.CreateMessage(assistantMessage);

            // Update session
            session.UpdateLastMessageAt();

            if (isNewSession)
            {
                var title = GenerateTitle(request.Message);
                session.UpdateTitle(title);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Record metrics
            AppMetrics.RecordNovaMessage(isNewSession, inputTokens, outputTokens);

            // Send completion event
            await WriteSSEData(httpContext, new
            {
                type = "done",
                messageId = assistantMessage.Id,
                createdAt = assistantMessage.CreatedAt
            });

            activity?.SetSuccess(true);
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            await WriteSSEError(httpContext, "An error occurred while generating the response");
        }
    }

    private static async Task WriteSSEData(HttpContext context, object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync($"data: {json}\n\n", Encoding.UTF8);
        await context.Response.Body.FlushAsync();
    }

    private static async Task WriteSSEError(HttpContext context, string message)
    {
        await WriteSSEData(context, new { type = "error", message });
    }

    private static string GenerateTitle(string firstMessage)
    {
        var title = firstMessage.Trim();

        if (title.Length <= 50)
        {
            return title;
        }

        var breakPoint = title.LastIndexOf(' ', 50);
        if (breakPoint < 20)
        {
            breakPoint = 50;
        }

        return title[..breakPoint].TrimEnd() + "...";
    }

    private static async Task<IResult> GetConversations(
        HttpContext httpContext,
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var profileId = httpContext.GetCurrentProfileId();

        // Parse cursor if provided (format: lastMessageAt|id)
        DateTimeOffset? beforeLastMessageAt = null;
        Guid? beforeId = null;

        if (!string.IsNullOrEmpty(cursor))
        {
            var parts = cursor.Split('|');
            if (parts.Length == 2)
            {
                if (DateTimeOffset.TryParse(parts[0], out var parsedDate))
                {
                    beforeLastMessageAt = parsedDate;
                }
                if (Guid.TryParse(parts[1], out var parsedId))
                {
                    beforeId = parsedId;
                }
            }
        }

        var query = new GetConversationsQuery(
            ProfileId: profileId,
            Limit: limit ?? 20,
            BeforeLastMessageAt: beforeLastMessageAt,
            BeforeId: beforeId);

        var result = await mediator.SendAsync(query, cancellationToken);

        // Generate next cursor from last item
        string? nextCursor = null;
        if (result.HasMore && result.Conversations.Count > 0)
        {
            var lastConversation = result.Conversations[^1];
            nextCursor = $"{lastConversation.LastMessageAt:O}|{lastConversation.Id}";
        }

        return Results.Ok(new
        {
            sessions = result.Conversations.Select(c => new
            {
                id = c.Id,
                title = c.Title,
                createdAt = c.CreatedAt,
                lastMessageAt = c.LastMessageAt,
                messageCount = c.MessageCount
            }),
            nextCursor,
            hasMore = result.HasMore
        });
    }

    private static async Task<IResult> GetConversation(
        HttpContext httpContext,
        Guid sessionId,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var profileId = httpContext.GetCurrentProfileId();

        var query = new GetConversationQuery(
            ProfileId: profileId,
            SessionId: sessionId);

        var result = await mediator.SendAsync(query, cancellationToken);

        return Results.Ok(new
        {
            id = result.Id,
            title = result.Title,
            createdAt = result.CreatedAt,
            lastMessageAt = result.LastMessageAt,
            messages = result.Messages.Select(m => new
            {
                id = m.Id,
                role = m.Role,
                content = m.Content,
                createdAt = m.CreatedAt
            })
        });
    }

    private static async Task<IResult> DeleteConversation(
        HttpContext httpContext,
        Guid sessionId,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var profileId = httpContext.GetCurrentProfileId();

        var command = new DeleteConversationCommand(
            ProfileId: profileId,
            SessionId: sessionId);

        await mediator.SendAsync(command, cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> GetLearnerProfile(
        HttpContext httpContext,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var profileId = httpContext.GetCurrentProfileId();

        var query = new GetLearnerProfileQuery(ProfileId: profileId);
        var result = await mediator.SendAsync(query, cancellationToken);

        return Results.Ok(new
        {
            id = result.Id,
            profileId = result.ProfileId,
            currentRole = result.CurrentRole,
            experienceYears = result.ExperienceYears,
            primaryTechStack = result.PrimaryTechStack,
            currentProject = result.CurrentProject,
            learningGoals = result.LearningGoals,
            learningStyle = result.LearningStyle,
            preferredPace = result.PreferredPace,
            identifiedStrengths = result.IdentifiedStrengths,
            identifiedStruggles = result.IdentifiedStruggles,
            hasUserData = result.HasUserData,
            createdAt = result.CreatedAt,
            updatedAt = result.UpdatedAt
        });
    }

    private static async Task<IResult> UpdateLearnerProfile(
        HttpContext httpContext,
        [FromBody] UpdateLearnerProfileRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var profileId = httpContext.GetCurrentProfileId();

        var command = new UpdateLearnerProfileCommand(
            ProfileId: profileId,
            CurrentRole: request.CurrentRole,
            ExperienceYears: request.ExperienceYears,
            PrimaryTechStack: request.PrimaryTechStack,
            CurrentProject: request.CurrentProject,
            LearningGoals: request.LearningGoals,
            LearningStyle: request.LearningStyle,
            PreferredPace: request.PreferredPace);

        var result = await mediator.SendAsync(command, cancellationToken);

        return Results.Ok(new
        {
            id = result.Id,
            profileId = result.ProfileId,
            currentRole = result.CurrentRole,
            experienceYears = result.ExperienceYears,
            primaryTechStack = result.PrimaryTechStack,
            currentProject = result.CurrentProject,
            learningGoals = result.LearningGoals,
            learningStyle = result.LearningStyle,
            preferredPace = result.PreferredPace,
            identifiedStrengths = result.IdentifiedStrengths,
            identifiedStruggles = result.IdentifiedStruggles,
            hasUserData = result.HasUserData,
            createdAt = result.CreatedAt,
            updatedAt = result.UpdatedAt
        });
    }

    private static async Task<IResult> TriggerMemoryExtraction(
        HttpContext httpContext,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var profileId = httpContext.GetCurrentProfileId();

        var command = new TriggerMemoryExtractionCommand(ProfileId: profileId);
        var result = await mediator.SendAsync(command, cancellationToken);

        return Results.Ok(new
        {
            sessionsProcessed = result.SessionsProcessed,
            totalMemoriesCreated = result.TotalMemoriesCreated
        });
    }

    // Memory retrieval constants for streaming
    private const int RecentMemoriesCount = 5;
    private const int HighImportanceThreshold = 4;
    private const int HighImportanceLimit = 5;
    private const int SemanticSearchLimit = 10;
    private const int MaxTotalMemories = 20;
    private const float MinSemanticSimilarity = 0.7f;

    private static async Task<IReadOnlyList<MemoryDto>> GetMemoriesForStreamingContextAsync(
        IUnitOfWork unitOfWork,
        IEmbeddingService embeddingService,
        Guid profileId,
        string currentMessage,
        CancellationToken cancellationToken)
    {
        // Get recent memories
        var recentMemories = await unitOfWork.NovaMemories.GetRecentAsync(
            profileId,
            RecentMemoriesCount,
            cancellationToken);

        // Get high-importance memories
        var highImportanceMemories = await unitOfWork.NovaMemories.GetByMinImportanceAsync(
            profileId,
            HighImportanceThreshold,
            HighImportanceLimit,
            cancellationToken);

        // Get semantically relevant memories
        var semanticMemories = await GetSemanticallySimilarMemoriesAsync(
            unitOfWork,
            embeddingService,
            profileId,
            currentMessage,
            cancellationToken);

        // Merge and deduplicate
        return MergeAndDeduplicateMemories(
            recentMemories,
            highImportanceMemories,
            semanticMemories);
    }

    private static async Task<List<(Memory Memory, float Similarity)>> GetSemanticallySimilarMemoriesAsync(
        IUnitOfWork unitOfWork,
        IEmbeddingService embeddingService,
        Guid profileId,
        string currentMessage,
        CancellationToken cancellationToken)
    {
        // Get all memories with embeddings
        var memoriesWithEmbeddings = await unitOfWork.NovaMemories.GetWithEmbeddingsAsync(
            profileId,
            cancellationToken);

        if (memoriesWithEmbeddings.Count == 0)
        {
            return [];
        }

        // Generate embedding for current message
        var messageEmbedding = await embeddingService.GetEmbeddingAsync(
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

            var memoryEmbedding = embeddingService.DeserializeEmbedding(memory.Embedding);
            var similarity = embeddingService.CosineSimilarity(messageEmbedding, memoryEmbedding);

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
}

// Request DTOs
public sealed record SendMessageRequest(
    string Message,
    Guid? SessionId = null
);

public sealed record UpdateLearnerProfileRequest(
    string? CurrentRole,
    int? ExperienceYears,
    string? PrimaryTechStack,
    string? CurrentProject,
    string? LearningGoals,
    string? LearningStyle,
    string? PreferredPace
);
