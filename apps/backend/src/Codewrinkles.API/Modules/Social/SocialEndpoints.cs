using Codewrinkles.Application.Social;
using Codewrinkles.API.Extensions;
using Codewrinkles.Domain.Social.Exceptions;
using Kommand.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Codewrinkles.API.Modules.Social;

public static class SocialEndpoints
{
    public static void MapSocialEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/social")
            .WithTags("Social");

        // Follow a user
        group.MapPost("{profileId:guid}/follow", FollowUser)
            .WithName("FollowUser")
            .RequireAuthorization();

        // Unfollow a user
        group.MapDelete("{profileId:guid}/follow", UnfollowUser)
            .WithName("UnfollowUser")
            .RequireAuthorization();

        // Get followers of a profile
        group.MapGet("{profileId:guid}/followers", GetFollowers)
            .WithName("GetFollowers");

        // Get who a profile is following
        group.MapGet("{profileId:guid}/following", GetFollowing)
            .WithName("GetFollowing");

        // Check if current user is following a profile
        group.MapGet("{profileId:guid}/is-following", IsFollowing)
            .WithName("IsFollowing")
            .RequireAuthorization();

        // Get suggested profiles to follow
        group.MapGet("suggestions", GetSuggestedProfiles)
            .WithName("GetSuggestedProfiles")
            .RequireAuthorization();
    }

    private static async Task<IResult> FollowUser(
        HttpContext httpContext,
        Guid profileId,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract current user's ProfileId from JWT
            var currentUserId = httpContext.GetCurrentProfileId();

            var command = new FollowUserCommand(
                FollowerId: currentUserId,
                FollowingId: profileId
            );

            var result = await mediator.SendAsync(command, cancellationToken);

            return Results.Ok(new { success = result.Success });
        }
        catch (FollowSelfException ex)
        {
            return Results.Problem(
                title: "Cannot Follow Self",
                detail: ex.Message,
                statusCode: 400
            );
        }
        catch (AlreadyFollowingException ex)
        {
            return Results.Problem(
                title: "Already Following",
                detail: ex.Message,
                statusCode: 400
            );
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

    private static async Task<IResult> UnfollowUser(
        HttpContext httpContext,
        Guid profileId,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract current user's ProfileId from JWT
            var currentUserId = httpContext.GetCurrentProfileId();

            var command = new UnfollowUserCommand(
                FollowerId: currentUserId,
                FollowingId: profileId
            );

            var result = await mediator.SendAsync(command, cancellationToken);

            return Results.Ok(new { success = result.Success });
        }
        catch (NotFollowingException ex)
        {
            return Results.Problem(
                title: "Not Following",
                detail: ex.Message,
                statusCode: 400
            );
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

    private static async Task<IResult> GetFollowers(
        HttpContext httpContext,
        Guid profileId,
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

            var query = new GetFollowersQuery(
                ProfileId: profileId,
                Cursor: cursor,
                Limit: limit
            );

            var result = await mediator.SendAsync(query, cancellationToken);

            return Results.Ok(new
            {
                followers = result.Followers,
                totalCount = result.TotalCount,
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

    private static async Task<IResult> GetFollowing(
        HttpContext httpContext,
        Guid profileId,
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

            var query = new GetFollowingQuery(
                ProfileId: profileId,
                Cursor: cursor,
                Limit: limit
            );

            var result = await mediator.SendAsync(query, cancellationToken);

            return Results.Ok(new
            {
                following = result.Following,
                totalCount = result.TotalCount,
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

    private static async Task<IResult> IsFollowing(
        HttpContext httpContext,
        Guid profileId,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract current user's ProfileId from JWT
            var currentUserId = httpContext.GetCurrentProfileId();

            var query = new IsFollowingQuery(
                FollowerId: currentUserId,
                FollowingId: profileId
            );

            var result = await mediator.SendAsync(query, cancellationToken);

            return Results.Ok(new { isFollowing = result.IsFollowing });
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

    private static async Task<IResult> GetSuggestedProfiles(
        HttpContext httpContext,
        [FromQuery] int limit,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract current user's ProfileId from JWT
            var currentUserId = httpContext.GetCurrentProfileId();

            // Default limit to 10 if not provided or invalid
            if (limit <= 0 || limit > 50)
            {
                limit = 10;
            }

            var query = new GetSuggestedProfilesQuery(
                CurrentUserId: currentUserId,
                Limit: limit
            );

            var result = await mediator.SendAsync(query, cancellationToken);

            return Results.Ok(new { suggestions = result.Suggestions });
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
