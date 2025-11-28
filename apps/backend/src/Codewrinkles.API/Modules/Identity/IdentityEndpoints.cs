using Codewrinkles.Application.Users;
using Codewrinkles.API.Extensions;
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

        group.MapPut("/profile/{profileId:guid}", UpdateProfile)
            .WithName("UpdateProfile")
            .RequireAuthorization("MustBeProfileOwner");

        group.MapPost("/profile/{profileId:guid}/avatar", UploadAvatar)
            .WithName("UploadAvatar")
            .RequireAuthorization("MustBeProfileOwner")
            .DisableAntiforgery();

        group.MapPost("/change-password", ChangePassword)
            .WithName("ChangePassword")
            .RequireAuthorization();
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
                bio = result.Bio,
                avatarUrl = result.AvatarUrl,
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

    private static async Task<IResult> UpdateProfile(
        Guid profileId,
        [FromBody] UpdateProfileRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateProfileCommand(
                ProfileId: profileId,
                Name: request.Name,
                Bio: request.Bio,
                Handle: request.Handle,
                Location: request.Location,
                WebsiteUrl: request.WebsiteUrl
            );

            var result = await mediator.SendAsync(command, cancellationToken);

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
        catch (ProfileNotFoundException)
        {
            return Results.NotFound();
        }
        catch (HandleAlreadyTakenException ex)
        {
            return Results.Problem(
                title: "Handle Already Taken",
                detail: ex.Message,
                statusCode: 409
            );
        }
    }

    private static async Task<IResult> UploadAvatar(
        Guid profileId,
        IFormFile file,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        // Validate file
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest("No file uploaded");
        }

        // Validate file size (max 5MB)
        const int maxFileSize = 5 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return Results.BadRequest("File size exceeds 5MB limit");
        }

        // Validate content type
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return Results.BadRequest("Invalid file type. Allowed: JPEG, PNG, GIF, WebP");
        }

        try
        {
            await using var stream = file.OpenReadStream();

            var command = new UploadAvatarCommand(
                ProfileId: profileId,
                ImageStream: stream
            );

            var result = await mediator.SendAsync(command, cancellationToken);

            return Results.Ok(new
            {
                profileId = result.ProfileId,
                avatarUrl = result.AvatarUrl
            });
        }
        catch (ProfileNotFoundException)
        {
            return Results.NotFound();
        }
        catch (InvalidImageException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private static async Task<IResult> ChangePassword(
        HttpContext httpContext,
        [FromBody] ChangePasswordRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract IdentityId from JWT claims (user can only change their own password)
            var identityId = httpContext.GetCurrentIdentityId();

            var command = new ChangePasswordCommand(
                IdentityId: identityId,
                CurrentPassword: request.CurrentPassword,
                NewPassword: request.NewPassword
            );

            await mediator.SendAsync(command, cancellationToken);

            return Results.Ok(new { success = true });
        }
        catch (IdentityNotFoundException)
        {
            return Results.NotFound();
        }
        catch (CurrentPasswordInvalidException)
        {
            return Results.Problem(
                title: "Invalid Password",
                detail: "Current password is incorrect",
                statusCode: 400
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

public sealed record UpdateProfileRequest(
    string Name,
    string? Bio,
    string? Handle,
    string? Location,
    string? WebsiteUrl
);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);
