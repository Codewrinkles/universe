using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Codewrinkles.API.Middleware;

/// <summary>
/// Middleware that enriches the current OpenTelemetry Activity with user identity from JWT claims.
/// Must be registered after UseAuthentication() so that HttpContext.User is populated.
/// Tags added here appear in Application Insights customDimensions for the requests table.
/// </summary>
public sealed class UserTelemetryMiddleware
{
    private readonly RequestDelegate _next;

    public UserTelemetryMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add user info to current activity BEFORE request processing continues
        // Authentication middleware has already run by this point in the pipeline
        if (Activity.Current is { } activity &&
            context.User.Identity?.IsAuthenticated == true)
        {
            var handle = context.User.FindFirst("handle")?.Value;
            var name = context.User.FindFirst(JwtRegisteredClaimNames.Name)?.Value
                ?? context.User.FindFirst(ClaimTypes.Name)?.Value;
            var email = context.User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                ?? context.User.FindFirst(ClaimTypes.Email)?.Value;
            var profileId = context.User.FindFirst("profileId")?.Value;

            if (!string.IsNullOrEmpty(handle))
            {
                activity.SetTag("user.handle", handle);
            }

            if (!string.IsNullOrEmpty(name))
            {
                activity.SetTag("user.name", name);
            }

            if (!string.IsNullOrEmpty(email))
            {
                activity.SetTag("user.email", email);
            }

            if (!string.IsNullOrEmpty(profileId))
            {
                activity.SetTag("user.profile_id", profileId);
            }
        }

        await _next(context);
    }
}
