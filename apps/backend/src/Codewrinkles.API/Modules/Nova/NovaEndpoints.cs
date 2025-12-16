using System.Text;
using System.Text.Json;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Application.Nova;
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
            .RequireAuthorization(); // All Nova endpoints require authentication

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
        CancellationToken cancellationToken)
    {
        const int MaxContextMessages = 20;

        var profileId = httpContext.GetCurrentProfileId();
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Nova.SendMessage);
        activity?.SetProfileId(profileId);

        // Set SSE headers
        httpContext.Response.ContentType = "text/event-stream";
        httpContext.Response.Headers.CacheControl = "no-cache";
        httpContext.Response.Headers.Connection = "keep-alive";

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
                await WriteSSEError(httpContext, "Conversation not found");
                return;
            }

            if (existingSession.ProfileId != profileId)
            {
                await WriteSSEError(httpContext, "Access denied");
                return;
            }

            session = existingSession;
        }
        else
        {
            session = ConversationSession.Create(profileId);
            unitOfWork.Nova.CreateSession(session);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            isNewSession = true;
        }

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

        // Build messages for LLM
        var llmMessages = new List<LlmMessage>
        {
            new(MessageRole.System, SystemPrompts.CodyCoach)
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

        try
        {
            await foreach (var chunk in llmService.GetStreamingChatCompletionAsync(llmMessages, cancellationToken))
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
}

// Request DTOs
public sealed record SendMessageRequest(
    string Message,
    Guid? SessionId = null
);
