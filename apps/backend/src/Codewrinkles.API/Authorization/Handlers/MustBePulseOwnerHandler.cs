using Codewrinkles.API.Authorization.Requirements;
using Codewrinkles.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using PulseEntity = Codewrinkles.Domain.Pulse.Pulse;

namespace Codewrinkles.API.Authorization.Handlers;

/// <summary>
/// Handles the MustBePulseOwnerRequirement by comparing the pulse's AuthorId
/// with the profileId claim from the JWT token.
/// Used with IAuthorizationService.AuthorizeAsync(user, pulse, "MustBePulseOwner").
/// </summary>
public sealed class MustBePulseOwnerHandler
    : AuthorizationHandler<MustBePulseOwnerRequirement, PulseEntity>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MustBePulseOwnerHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MustBePulseOwnerRequirement requirement,
        PulseEntity pulse)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            return Task.CompletedTask; // Fail authorization
        }

        // Extract ProfileId from JWT claims
        var currentProfileId = httpContext.GetCurrentProfileIdOrNull();
        if (currentProfileId is null)
        {
            return Task.CompletedTask; // Fail authorization - not authenticated
        }

        // Compare with pulse's AuthorId
        if (pulse.AuthorId == currentProfileId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
