using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Codewrinkles.Telemetry;

namespace Codewrinkles.API.Telemetry;

/// <summary>
/// Enriches HTTP request activities with user identity from JWT claims.
/// This ensures Application Insights requests table includes user info for dashboards.
/// </summary>
public static class UserIdentityEnricher
{
    /// <summary>
    /// Enriches the activity with user identity extracted from the HttpContext.
    /// Called by ASP.NET Core OpenTelemetry instrumentation for each request.
    /// </summary>
    public static void EnrichWithUserIdentity(Activity activity, HttpContext context)
    {
        // Get human-readable user info for dashboards
        var handle = context.User.FindFirst("handle")?.Value;
        var name = context.User.FindFirst(JwtRegisteredClaimNames.Name)?.Value
            ?? context.User.FindFirst(ClaimTypes.Name)?.Value;
        var email = context.User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
            ?? context.User.FindFirst(ClaimTypes.Email)?.Value;

        // Set handle as the primary user identifier (human-readable, public info)
        if (!string.IsNullOrEmpty(handle))
        {
            activity.SetTag("user.handle", handle);
            activity.SetTag("enduser.id", handle); // Populates user_Id in App Insights
        }

        // Set name for display
        if (!string.IsNullOrEmpty(name))
        {
            activity.SetTag("user.name", name);
        }

        // Set email (for identifying users in dashboard)
        if (!string.IsNullOrEmpty(email))
        {
            activity.SetTag("user.email", email);
        }

        // Also keep profile_id for data joins if needed
        var profileIdClaim = context.User.FindFirst("profileId")?.Value;
        if (!string.IsNullOrEmpty(profileIdClaim))
        {
            activity.SetTag(TagNames.User.ProfileId, profileIdClaim);
        }
    }
}
