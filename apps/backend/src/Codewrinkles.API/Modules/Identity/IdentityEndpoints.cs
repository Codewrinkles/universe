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

        group.MapPost("/login", LoginUser)
            .WithName("LoginUser");
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

    private static async Task<IResult> LoginUser(
        [FromBody] LoginUserRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new LoginUserCommand(
                Email: request.Email,
                Password: request.Password
            );

            var result = await mediator.SendAsync(command, cancellationToken);

            return Results.Ok(new
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
        catch (InvalidCredentialsException)
        {
            return Results.Unauthorized();
        }
        catch (AccountSuspendedException ex)
        {
            return Results.Problem(
                title: "Account Suspended",
                detail: ex.Message,
                statusCode: 403
            );
        }
        catch (AccountLockedException ex)
        {
            return Results.Problem(
                title: "Account Locked",
                detail: ex.Message,
                statusCode: 423
            );
        }
    }
}

public sealed record RegisterUserRequest(
    string Email,
    string Password,
    string Name,
    string? Handle = null
);

public sealed record LoginUserRequest(
    string Email,
    string Password
);
