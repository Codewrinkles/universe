using System.Diagnostics;
using OpenTelemetry;

namespace Codewrinkles.API.Telemetry;

/// <summary>
/// Enriches OpenTelemetry activities with authenticated user information.
/// This allows tracking user sessions in Application Insights via customDimensions.
/// </summary>
public sealed class UserTelemetryProcessor : BaseProcessor<Activity>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserTelemetryProcessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override void OnEnd(Activity activity)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.User?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var handle = context.User.FindFirst("handle")?.Value;
        var email = context.User.FindFirst("email")?.Value;
        var name = context.User.FindFirst("name")?.Value;
        var profileId = context.User.FindFirst("profileId")?.Value;

        if (!string.IsNullOrEmpty(handle))
        {
            activity.SetTag("user.handle", handle);
        }

        if (!string.IsNullOrEmpty(email))
        {
            activity.SetTag("user.email", email);
        }

        if (!string.IsNullOrEmpty(name))
        {
            activity.SetTag("user.name", name);
        }

        if (!string.IsNullOrEmpty(profileId))
        {
            activity.SetTag("user.profile_id", profileId);
        }
    }
}
