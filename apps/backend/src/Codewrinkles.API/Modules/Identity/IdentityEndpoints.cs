using Codewrinkles.Application.Users;
using Kommand.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Codewrinkles.API.Modules.Identity;

public static class IdentityEndpoints
{
    public static void MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/identity")
            .WithTags("Identity");

        group.MapPost("/register", RegisterUser)
            .WithName("RegisterUser");
    }

    private static async Task<IResult> RegisterUser(
        [FromBody] RegisterUserRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(
            Email: request.Email,
            Password: request.Password,
            Name: request.Name,
            Handle: request.Handle
        );

        var result = await mediator.SendAsync(command, cancellationToken);

        return Results.Created($"/api/identity/{result.IdentityId}", new
        {
            identityId = result.IdentityId,
            profileId = result.ProfileId,
            email = result.Email,
            name = result.Name,
            handle = result.Handle,
            accessToken = result.AccessToken,
            refreshToken = result.RefreshToken
        });
    }
}

public sealed record RegisterUserRequest(
    string Email,
    string Password,
    string Name,
    string? Handle = null
);
