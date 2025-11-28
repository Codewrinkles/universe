using Codewrinkles.API.Authorization.Requirements;
using Codewrinkles.API.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace Codewrinkles.API.Authorization.Handlers;

/// <summary>
/// Handles the MustBeProfileOwnerRequirement by comparing the profileId
/// route parameter with the profileId claim from the JWT token.
/// </summary>
public sealed class MustBeProfileOwnerHandler
    : AuthorizationHandler<MustBeProfileOwnerRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MustBeProfileOwnerHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MustBeProfileOwnerRequirement requirement)
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
            return Task.CompletedTask; // Fail authorization - no claim
        }

        // Extract profileId from route parameter
        var routeProfileId = httpContext.GetRouteValueAsGuid("profileId");
        if (routeProfileId is null)
        {
            return Task.CompletedTask; // Fail authorization - no route param
        }

        // Compare the two
        if (currentProfileId == routeProfileId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
