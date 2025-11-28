using Codewrinkles.Application.Users;
using Kommand.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Codewrinkles.API.Modules.Identity;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profile")
            .WithTags("Profile");

        group.MapGet("/handle/{handle}", GetProfileByHandle)
            .WithName("GetProfileByHandle");

        group.MapGet("/search", SearchHandles)
            .WithName("SearchHandles");
    }

    private static async Task<IResult> GetProfileByHandle(
        string handle,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetProfileByHandleQuery(Handle: handle);
            var result = await mediator.SendAsync(query, cancellationToken);

            return Results.Ok(new
            {
                profileId = result.ProfileId,
                name = result.Name,
                handle = result.Handle,
                bio = result.Bio,
                avatarUrl = result.AvatarUrl,
                location = result.Location,
                websiteUrl = result.WebsiteUrl
            });
        }
        catch (ProfileNotFoundByHandleException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> SearchHandles(
        [FromQuery] string q,
        [FromQuery] int limit,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        // Validate query parameter
        if (string.IsNullOrWhiteSpace(q))
        {
            return Results.BadRequest(new { error = "Search term is required" });
        }

        // Enforce limit bounds
        var effectiveLimit = Math.Clamp(limit > 0 ? limit : 10, 1, 20);

        var query = new SearchHandlesQuery(q, effectiveLimit);
        var result = await mediator.SendAsync(query, cancellationToken);

        return Results.Ok(new
        {
            handles = result.Handles.Select(h => new
            {
                profileId = h.ProfileId,
                handle = h.Handle,
                name = h.Name,
                avatarUrl = h.AvatarUrl
            })
        });
    }
}
