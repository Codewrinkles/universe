using Codewrinkles.Application.Pulse;
using Codewrinkles.Domain.Pulse.Exceptions;
using Kommand.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Codewrinkles.API.Modules.Pulse;

public static class PulseEndpoints
{
    public static void MapPulseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pulse")
            .WithTags("Pulse");

        group.MapPost("", CreatePulse)
            .WithName("CreatePulse");

        group.MapGet("", GetFeed)
            .WithName("GetFeed");

        group.MapGet("{id:guid}", GetPulse)
            .WithName("GetPulse");
    }

    private static async Task<IResult> CreatePulse(
        [FromBody] CreatePulseRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreatePulseCommand(
                AuthorId: request.AuthorId,
                Content: request.Content
            );

            var result = await mediator.SendAsync(command, cancellationToken);

            return Results.Created($"/api/pulse/{result.PulseId}", new
            {
                pulseId = result.PulseId,
                content = result.Content,
                createdAt = result.CreatedAt
            });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Invalid Operation",
                detail: ex.Message,
                statusCode: 400
            );
        }
    }

    private static async Task<IResult> GetFeed(
        [FromQuery] string? cursor,
        [FromQuery] int limit,
        [FromQuery] Guid? currentUserId,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            // Default limit to 20 if not provided or invalid
            if (limit <= 0 || limit > 100)
            {
                limit = 20;
            }

            var query = new GetFeedQuery(
                CurrentUserId: currentUserId,
                Cursor: cursor,
                Limit: limit
            );

            var result = await mediator.SendAsync(query, cancellationToken);

            return Results.Ok(new
            {
                pulses = result.Pulses,
                nextCursor = result.NextCursor,
                hasMore = result.HasMore
            });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Invalid Cursor",
                detail: ex.Message,
                statusCode: 400
            );
        }
    }

    private static async Task<IResult> GetPulse(
        Guid id,
        [FromQuery] Guid? currentUserId,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetPulseQuery(
                PulseId: id,
                CurrentUserId: currentUserId
            );

            var result = await mediator.SendAsync(query, cancellationToken);

            return Results.Ok(result);
        }
        catch (PulseNotFoundException)
        {
            return Results.NotFound();
        }
    }
}

public sealed record CreatePulseRequest(
    Guid AuthorId,
    string Content
);
