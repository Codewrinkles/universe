using Microsoft.AspNetCore.Authorization;
using PulseEntity = Codewrinkles.Domain.Pulse.Pulse;

namespace Codewrinkles.API.Authorization.Requirements;

/// <summary>
/// Authorization requirement that validates the user owns the pulse resource.
/// Used with IAuthorizationService for resource-based authorization.
/// </summary>
public sealed class MustBePulseOwnerRequirement : IAuthorizationRequirement
{
}
