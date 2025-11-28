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
}
