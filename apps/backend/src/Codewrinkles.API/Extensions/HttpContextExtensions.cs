using System.Security.Claims;

namespace Codewrinkles.API.Extensions;

public static class HttpContextExtensions
{
    /// <summary>
    /// Extracts the ProfileId from JWT claims.
    /// Throws if the user is not authenticated or the claim is missing.
    /// </summary>
    public static Guid GetCurrentProfileId(this HttpContext context)
    {
        var profileIdClaim = context.User.FindFirst("profileId")?.Value
            ?? throw new UnauthorizedAccessException("ProfileId claim not found");

        if (!Guid.TryParse(profileIdClaim, out var profileId))
        {
            throw new UnauthorizedAccessException("Invalid ProfileId claim format");
        }

        return profileId;
    }

    /// <summary>
    /// Extracts the ProfileId from JWT claims if present.
    /// Returns null if the user is not authenticated or the claim is missing.
    /// Used for optional authentication scenarios (e.g., public feeds).
    /// </summary>
    public static Guid? GetCurrentProfileIdOrNull(this HttpContext context)
    {
        var profileIdClaim = context.User.FindFirst("profileId")?.Value;

        if (string.IsNullOrEmpty(profileIdClaim))
        {
            return null;
        }

        return Guid.TryParse(profileIdClaim, out var profileId) ? profileId : null;
    }

    /// <summary>
    /// Extracts the IdentityId from JWT claims (sub claim).
    /// Throws if the user is not authenticated or the claim is missing.
    /// </summary>
    public static Guid GetCurrentIdentityId(this HttpContext context)
    {
        var identityIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("Identity claim not found");

        if (!Guid.TryParse(identityIdClaim, out var identityId))
        {
            throw new UnauthorizedAccessException("Invalid IdentityId claim format");
        }

        return identityId;
    }

    /// <summary>
    /// Extracts a Guid value from route parameters.
    /// </summary>
    public static Guid? GetRouteValueAsGuid(this HttpContext context, string key)
    {
        var value = context.GetRouteValue(key)?.ToString();
        return Guid.TryParse(value, out var guid) ? guid : null;
    }
}
