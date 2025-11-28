using Microsoft.AspNetCore.Authorization;

namespace Codewrinkles.API.Authorization.Requirements;

/// <summary>
/// Authorization requirement that validates the user owns the profile
/// specified in the route parameter "profileId".
/// </summary>
public sealed class MustBeProfileOwnerRequirement : IAuthorizationRequirement
{
}
