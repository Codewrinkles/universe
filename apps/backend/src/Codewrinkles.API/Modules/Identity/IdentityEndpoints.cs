using System.Text.Json;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Application.Users;
using Codewrinkles.API.Extensions;
using Codewrinkles.Domain.Identity;
using Codewrinkles.Infrastructure.Services;
using Kommand.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

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

        group.MapPost("/refresh", RefreshAccessToken)
            .WithName("RefreshAccessToken");

        // PHASE 1: New RESTful endpoint for getting current user's profile
        // GET /api/identity/profile (no ID - extracted from JWT)
        group.MapGet("/profile", GetCurrentUserProfile)
            .WithName("GetCurrentUserProfile")
            .RequireAuthorization();

        // PHASE 2 TODO: Migrate these endpoints to RESTful pattern (no profileId in URL):
        // - PUT /api/identity/profile (instead of /profile/{profileId})
        // - POST /api/identity/profile/avatar (instead of /profile/{profileId}/avatar)
        // Keep old endpoints for backward compatibility, mark as deprecated
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

        group.MapGet("/onboarding/status", GetOnboardingStatus)
            .WithName("GetOnboardingStatus")
            .RequireAuthorization();

        group.MapPost("/onboarding/complete", CompleteOnboarding)
            .WithName("CompleteOnboarding")
            .RequireAuthorization();

        group.MapPost("/oauth/{provider}/initiate", InitiateOAuth)
            .WithName("InitiateOAuthFlow");

        group.MapGet("/oauth/{provider}/callback", HandleOAuthCallback)
            .WithName("HandleOAuthCallback");
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
            role = result.Role.ToString(),
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
                role = result.Role.ToString(),
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

    private static async Task<IResult> RefreshAccessToken(
        [FromBody] RefreshAccessTokenRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new RefreshAccessTokenCommand(
                RefreshToken: request.RefreshToken
            );

            var result = await mediator.SendAsync(command, cancellationToken);

            return Results.Ok(new
            {
                accessToken = result.AccessToken,
                refreshToken = result.RefreshToken
            });
        }
        catch (InvalidRefreshTokenException ex)
        {
            return Results.Problem(
                title: "Invalid Refresh Token",
                detail: ex.Message,
                statusCode: 401
            );
        }
        catch (RefreshTokenExpiredException ex)
        {
            return Results.Problem(
                title: "Refresh Token Expired",
                detail: ex.Message,
                statusCode: 401
            );
        }
    }

    /// <summary>
    /// PHASE 1: Get current authenticated user's profile
    /// RESTful endpoint - no ID in URL, extracted from JWT token
    /// Used by Settings page to load current profile data for editing
    /// </summary>
    private static async Task<IResult> GetCurrentUserProfile(
        HttpContext httpContext,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var identityId = httpContext.GetCurrentIdentityId();

            var query = new GetCurrentUserProfileQuery(IdentityId: identityId);
            var result = await mediator.SendAsync(query, cancellationToken);

            return Results.Ok(new
            {
                profileId = result.ProfileId,
                name = result.Name,
                handle = result.Handle,
                bio = result.Bio,
                avatarUrl = result.AvatarUrl,
                location = result.Location,
                websiteUrl = result.WebsiteUrl,
                onboardingCompleted = result.OnboardingCompleted
            });
        }
        catch (CurrentUserProfileNotFoundException)
        {
            return Results.NotFound();
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

    private static async Task<IResult> GetOnboardingStatus(
        HttpContext httpContext,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var profileId = httpContext.GetCurrentProfileId();

        var query = new GetOnboardingStatusQuery(profileId);
        var result = await mediator.SendAsync(query, cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> CompleteOnboarding(
        HttpContext httpContext,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var profileId = httpContext.GetCurrentProfileId();

        var command = new CompleteOnboardingCommand(profileId);
        await mediator.SendAsync(command, cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> InitiateOAuth(
        string provider,
        [FromBody] InitiateOAuthRequest request,
        [FromServices] IOAuthService oauthService,
        [FromServices] IDistributedCache cache,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OAuthProvider>(provider, true, out var oauthProvider))
        {
            return Results.BadRequest(new { error = "Invalid OAuth provider" });
        }

        var state = OAuthService.GenerateState();
        var cacheKey = $"oauth_state_{state}";
        var cacheValue = new OAuthStateData
        {
            Provider = oauthProvider,
            CreatedAt = DateTime.UtcNow,
            RedirectUri = request.RedirectUri
        };

        await cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(cacheValue),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            },
            cancellationToken);

        var backendCallbackUri = $"{request.BaseUrl}/api/identity/oauth/{provider.ToLowerInvariant()}/callback";
        var authUrl = oauthService.GenerateAuthorizationUrl(oauthProvider, backendCallbackUri, state);

        return Results.Ok(new { authorizationUrl = authUrl.Url });
    }

    private static async Task<IResult> HandleOAuthCallback(
        string provider,
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromServices] IMediator mediator,
        [FromServices] IDistributedCache cache,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OAuthProvider>(provider, true, out var oauthProvider))
        {
            return Results.Redirect($"{GetFrontendUrl()}/auth/error?message=Invalid+provider");
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            return Results.Redirect($"{GetFrontendUrl()}/auth/error?message={Uri.EscapeDataString(error)}");
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        {
            return Results.Redirect($"{GetFrontendUrl()}/auth/error?message=Missing+code+or+state");
        }

        var cacheKey = $"oauth_state_{state}";
        var storedStateJson = await cache.GetStringAsync(cacheKey, cancellationToken);

        if (storedStateJson is null)
        {
            return Results.Redirect($"{GetFrontendUrl()}/auth/error?message=Invalid+state");
        }

        var storedState = JsonSerializer.Deserialize<OAuthStateData>(storedStateJson);

        if (storedState is null || storedState.Provider != oauthProvider)
        {
            return Results.Redirect($"{GetFrontendUrl()}/auth/error?message=Provider+mismatch");
        }

        await cache.RemoveAsync(cacheKey, cancellationToken);

        var redirectUri = $"{GetBackendUrl()}/api/identity/oauth/{provider.ToLowerInvariant()}/callback";

        var command = new CompleteOAuthCallbackCommand(oauthProvider, code, state, redirectUri);

        try
        {
            var result = await mediator.SendAsync(command, cancellationToken);

            var frontendSuccessUrl = $"{storedState.RedirectUri}" +
                $"?access_token={Uri.EscapeDataString(result.AccessToken)}" +
                $"&refresh_token={Uri.EscapeDataString(result.RefreshToken)}" +
                $"&is_new_user={result.IsNewUser.ToString().ToLowerInvariant()}";

            return Results.Redirect(frontendSuccessUrl);
        }
        catch (Exception ex)
        {
            return Results.Redirect($"{GetFrontendUrl()}/auth/error?message={Uri.EscapeDataString(ex.Message)}");
        }
    }

    private static string GetBackendUrl() =>
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
            ? "https://localhost:7280"
            : "https://app-codwrinkles-api.azurewebsites.net";

    private static string GetFrontendUrl() =>
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
            ? "http://localhost:5173"
            : "https://codewrinkles.com";
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

public sealed record RefreshAccessTokenRequest(
    string RefreshToken
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

public sealed record InitiateOAuthRequest(string BaseUrl, string RedirectUri);

internal sealed record OAuthStateData
{
    public required OAuthProvider Provider { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required string RedirectUri { get; init; }
}
