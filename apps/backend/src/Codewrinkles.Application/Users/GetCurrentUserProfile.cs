using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Users;

/// <summary>
/// Query to get the current authenticated user's profile
/// IdentityId is extracted from JWT token in the endpoint handler
/// </summary>
public sealed record GetCurrentUserProfileQuery(
    Guid IdentityId
) : ICommand<CurrentUserProfileDto>;

/// <summary>
/// DTO representing the current user's profile data
/// Used for profile editing in Settings page
/// </summary>
public sealed record CurrentUserProfileDto(
    Guid ProfileId,
    string Name,
    string? Handle,
    string? Bio,
    string? AvatarUrl,
    string? Location,
    string? WebsiteUrl,
    bool OnboardingCompleted
);

/// <summary>
/// Exception thrown when profile is not found for the given identity
/// </summary>
public sealed class CurrentUserProfileNotFoundException : Exception
{
    public CurrentUserProfileNotFoundException(Guid identityId)
        : base($"Profile not found for identity {identityId}") { }
}

/// <summary>
/// Handler for getting the current user's profile
/// </summary>
public sealed class GetCurrentUserProfileQueryHandler
    : ICommandHandler<GetCurrentUserProfileQuery, CurrentUserProfileDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCurrentUserProfileQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CurrentUserProfileDto> HandleAsync(
        GetCurrentUserProfileQuery query,
        CancellationToken cancellationToken)
    {
        var profile = await _unitOfWork.Profiles.FindByIdentityIdAsync(
            query.IdentityId,
            cancellationToken);

        if (profile is null)
        {
            throw new CurrentUserProfileNotFoundException(query.IdentityId);
        }

        return new CurrentUserProfileDto(
            ProfileId: profile.Id,
            Name: profile.Name,
            Handle: profile.Handle,
            Bio: profile.Bio,
            AvatarUrl: profile.AvatarUrl,
            Location: profile.Location,
            WebsiteUrl: profile.WebsiteUrl,
            OnboardingCompleted: profile.OnboardingCompleted
        );
    }
}
