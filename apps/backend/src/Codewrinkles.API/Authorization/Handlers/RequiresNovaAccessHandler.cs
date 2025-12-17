using Codewrinkles.API.Authorization.Requirements;
using Codewrinkles.API.Extensions;
using Codewrinkles.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Codewrinkles.API.Authorization.Handlers;

/// <summary>
/// Handles the RequiresNovaAccessRequirement by checking if the user's
/// profile has Nova access enabled.
/// </summary>
public sealed class RequiresNovaAccessHandler
    : AuthorizationHandler<RequiresNovaAccessRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUnitOfWork _unitOfWork;

    public RequiresNovaAccessHandler(
        IHttpContextAccessor httpContextAccessor,
        IUnitOfWork unitOfWork)
    {
        _httpContextAccessor = httpContextAccessor;
        _unitOfWork = unitOfWork;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RequiresNovaAccessRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            return; // Fail authorization
        }

        // Extract ProfileId from JWT claims
        var profileId = httpContext.GetCurrentProfileIdOrNull();
        if (profileId is null)
        {
            return; // Fail authorization - no claim
        }

        // Load profile from database to check Nova access
        var profile = await _unitOfWork.Profiles.FindByIdAsync(
            profileId.Value,
            httpContext.RequestAborted);

        if (profile is null)
        {
            return; // Fail authorization - profile not found
        }

        // Check if user has Nova access
        if (profile.HasNovaAccess)
        {
            context.Succeed(requirement);
        }
    }
}
