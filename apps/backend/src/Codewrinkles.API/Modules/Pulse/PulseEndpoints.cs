using Codewrinkles.Application.Pulse;
using Codewrinkles.API.Extensions;
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
            .WithName("CreatePulse")
            .RequireAuthorization()
            .DisableAntiforgery();

        group.MapGet("", GetFeed)
            .WithName("GetFeed");

        group.MapGet("{id:guid}", GetPulse)
            .WithName("GetPulse");

        group.MapPost("{id:guid}/like", LikePulse)
            .WithName("LikePulse")
            .RequireAuthorization();

        group.MapDelete("{id:guid}/like", UnlikePulse)
            .WithName("UnlikePulse")
            .RequireAuthorization();
    }

    private static async Task<IResult> CreatePulse(
        HttpContext httpContext,
        [FromForm] string content,
        [FromForm] IFormFile? image,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract ProfileId from JWT claims (user can only create pulses as themselves)
            var authorId = httpContext.GetCurrentProfileId();

            // Open image stream if provided
            Stream? imageStream = null;
            if (image is not null)
            {
                imageStream = image.OpenReadStream();
            }

            var command = new CreatePulseCommand(
                AuthorId: authorId,
                Content: content,
                ImageStream: imageStream
            );

            var result = await mediator.SendAsync(command, cancellationToken);

            return Results.Created($"/api/pulse/{result.PulseId}", new
            {
                pulseId = result.PulseId,
                content = result.Content,
                createdAt = result.CreatedAt,
                imageUrl = result.ImageUrl
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
        finally
        {
            // Dispose the image stream if it was opened
            if (image is not null)
            {
                await image.OpenReadStream().DisposeAsync();
            }
        }
    }

    private static async Task<IResult> GetFeed(
        HttpContext httpContext,
        [FromQuery] string? cursor,
        [FromQuery] int limit,
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

            // Extract ProfileId from JWT if present (optional auth for feeds)
            var currentUserId = httpContext.GetCurrentProfileIdOrNull();

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
        HttpContext httpContext,
        Guid id,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract ProfileId from JWT if present (optional auth)
            var currentUserId = httpContext.GetCurrentProfileIdOrNull();

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

    private static async Task<IResult> LikePulse(
        HttpContext httpContext,
        Guid id,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract ProfileId from JWT claims (user can only like as themselves)
            var profileId = httpContext.GetCurrentProfileId();

            var command = new LikePulseCommand(
                PulseId: id,
                ProfileId: profileId
            );

            var result = await mediator.SendAsync(command, cancellationToken);

            return Results.Ok(new { success = result.Success });
        }
        catch (PulseNotFoundException)
        {
            return Results.NotFound();
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

    private static async Task<IResult> UnlikePulse(
        HttpContext httpContext,
        Guid id,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract ProfileId from JWT claims (user can only unlike as themselves)
            var profileId = httpContext.GetCurrentProfileId();

            var command = new UnlikePulseCommand(
                PulseId: id,
                ProfileId: profileId
            );

            var result = await mediator.SendAsync(command, cancellationToken);

            return Results.Ok(new { success = result.Success });
        }
        catch (PulseNotFoundException)
        {
            return Results.NotFound();
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
}
