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

        group.MapGet("author/{authorId:guid}", GetPulsesByAuthor)
            .WithName("GetPulsesByAuthor");

        group.MapGet("{id:guid}", GetPulse)
            .WithName("GetPulse");

        group.MapDelete("{id:guid}", DeletePulse)
            .WithName("DeletePulse")
            .RequireAuthorization();

        group.MapPost("{id:guid}/like", LikePulse)
            .WithName("LikePulse")
            .RequireAuthorization();

        group.MapDelete("{id:guid}/like", UnlikePulse)
            .WithName("UnlikePulse")
            .RequireAuthorization();

        group.MapPost("repulse", CreateRepulse)
            .WithName("CreateRepulse")
            .RequireAuthorization()
            .DisableAntiforgery();

        group.MapPost("{parentId:guid}/reply", CreateReply)
            .WithName("CreateReply")
            .RequireAuthorization()
            .DisableAntiforgery();

        group.MapGet("{id:guid}/thread", GetThread)
            .WithName("GetThread");

        group.MapGet("hashtags/trending", GetTrendingHashtags)
            .WithName("GetTrendingHashtags");

        group.MapGet("hashtags/{tag}", GetPulsesByHashtag)
            .WithName("GetPulsesByHashtag");
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

    private static async Task<IResult> GetPulsesByAuthor(
        HttpContext httpContext,
        Guid authorId,
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

            // Extract ProfileId from JWT if present (optional auth)
            var currentUserId = httpContext.GetCurrentProfileIdOrNull();

            var query = new GetPulsesByAuthorQuery(
                AuthorId: authorId,
                CurrentUserId: currentUserId,
                Cursor: cursor,
                Limit: limit
            );

            var result = await mediator.SendAsync(query, cancellationToken);

            return Results.Ok(new
            {
                pulses = result.Pulses,
                nextCursor = result.NextCursor,
                hasMore = result.HasMore,
                totalCount = result.TotalCount
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
        // Extract ProfileId from JWT if present (optional auth)
        var currentUserId = httpContext.GetCurrentProfileIdOrNull();

        var query = new GetPulseQuery(
            PulseId: id,
            CurrentUserId: currentUserId
        );

        var result = await mediator.SendAsync(query, cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> LikePulse(
        HttpContext httpContext,
        Guid id,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
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

    private static async Task<IResult> UnlikePulse(
        HttpContext httpContext,
        Guid id,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
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

    private static async Task<IResult> CreateReply(
        HttpContext httpContext,
        Guid parentId,
        [FromForm] string content,
        [FromForm] IFormFile? image,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        // Extract ProfileId from JWT claims (user can only create replies as themselves)
        var authorId = httpContext.GetCurrentProfileId();

        // Open image stream if provided
        Stream? imageStream = null;
        try
        {
            if (image is not null)
            {
                imageStream = image.OpenReadStream();
            }

            var command = new CreateReplyCommand(
                AuthorId: authorId,
                ParentPulseId: parentId,
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
        finally
        {
            // Dispose the image stream if it was opened
            if (imageStream is not null)
            {
                await imageStream.DisposeAsync();
            }
        }
    }

    private static async Task<IResult> GetThread(
        HttpContext httpContext,
        Guid id,
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

            // Extract ProfileId from JWT if present (optional auth)
            var currentUserId = httpContext.GetCurrentProfileIdOrNull();

            // Parse cursor if provided
            DateTime? beforeCreatedAt = null;
            Guid? beforeId = null;

            if (!string.IsNullOrWhiteSpace(cursor))
            {
                var parts = cursor.Split('_');
                if (parts.Length == 2 &&
                    DateTime.TryParse(parts[0], out var parsedDate) &&
                    Guid.TryParse(parts[1], out var parsedId))
                {
                    beforeCreatedAt = parsedDate;
                    beforeId = parsedId;
                }
                else
                {
                    return Results.Problem(
                        title: "Invalid Cursor",
                        detail: "Cursor format is invalid. Expected format: ISO8601DateTime_Guid",
                        statusCode: 400
                    );
                }
            }

            var query = new GetThreadQuery(
                PulseId: id,
                CurrentUserId: currentUserId,
                Limit: limit,
                BeforeCreatedAt: beforeCreatedAt,
                BeforeId: beforeId
            );

            var result = await mediator.SendAsync(query, cancellationToken);

            return Results.Ok(new
            {
                parentPulse = result.ParentPulse,
                replies = result.Replies,
                totalReplyCount = result.TotalReplyCount,
                nextCursor = result.NextCursor,
                hasMore = result.HasMore
            });
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

    private static async Task<IResult> CreateRepulse(
        HttpContext httpContext,
        [FromForm] string content,
        [FromForm] Guid repulsedPulseId,
        [FromForm] IFormFile? image,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        // Extract ProfileId from JWT claims (user can only create repulses as themselves)
        var authorId = httpContext.GetCurrentProfileId();

        // Open image stream if provided
        Stream? imageStream = null;
        try
        {
            if (image is not null)
            {
                imageStream = image.OpenReadStream();
            }

            var command = new CreateRepulseCommand(
                AuthorId: authorId,
                RepulsedPulseId: repulsedPulseId,
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
        finally
        {
            // Dispose the image stream if it was opened
            if (imageStream is not null)
            {
                await imageStream.DisposeAsync();
            }
        }
    }

    private static async Task<IResult> DeletePulse(
        HttpContext httpContext,
        Guid id,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        // Extract ProfileId from JWT claims (user can only delete their own pulses)
        var profileId = httpContext.GetCurrentProfileId();

        var command = new DeletePulseCommand(
            ProfileId: profileId,
            PulseId: id
        );

        var result = await mediator.SendAsync(command, cancellationToken);

        return Results.Ok(new { success = result.Success });
    }

    private static async Task<IResult> GetTrendingHashtags(
        [FromQuery] int limit,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        // Default limit to 10 if not provided or invalid
        if (limit <= 0 || limit > 50)
        {
            limit = 10;
        }

        var query = new GetTrendingHashtagsQuery(Limit: limit);
        var result = await mediator.SendAsync(query, cancellationToken);

        return Results.Ok(new { hashtags = result });
    }

    private static async Task<IResult> GetPulsesByHashtag(
        HttpContext httpContext,
        string tag,
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

            // Extract ProfileId from JWT if present (optional auth)
            var currentUserId = httpContext.GetCurrentProfileIdOrNull();

            var query = new GetPulsesByHashtagQuery(
                Tag: tag,
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
}
