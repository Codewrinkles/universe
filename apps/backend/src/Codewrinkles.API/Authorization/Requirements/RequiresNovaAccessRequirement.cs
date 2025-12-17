using Microsoft.AspNetCore.Authorization;

namespace Codewrinkles.API.Authorization.Requirements;

/// <summary>
/// Authorization requirement that validates the user has access to Nova.
/// Checks the user's NovaAccess property on their Profile.
/// </summary>
public sealed class RequiresNovaAccessRequirement : IAuthorizationRequirement
{
}
