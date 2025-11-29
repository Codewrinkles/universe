using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Users;

public sealed record GetOnboardingStatusQuery(Guid ProfileId) : ICommand<OnboardingStatusResponse>;

public sealed record OnboardingStatusResponse(
    bool IsCompleted,
    bool HasHandle,
    bool HasBio,
    bool HasAvatar,
    bool HasPostedPulse,
    int FollowingCount);

public sealed class GetOnboardingStatusQueryHandler
    : ICommandHandler<GetOnboardingStatusQuery, OnboardingStatusResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetOnboardingStatusQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OnboardingStatusResponse> HandleAsync(
        GetOnboardingStatusQuery query,
        CancellationToken cancellationToken)
    {
        var profile = await _unitOfWork.Profiles.FindByIdAsync(
            query.ProfileId,
            cancellationToken);

        if (profile == null)
        {
            return new OnboardingStatusResponse(
                IsCompleted: false,
                HasHandle: false,
                HasBio: false,
                HasAvatar: false,
                HasPostedPulse: false,
                FollowingCount: 0);
        }

        // Check if user has posted at least one pulse
        var pulseCount = await _unitOfWork.Pulses.GetPulseCountByAuthorAsync(
            profile.Id,
            cancellationToken);

        // Check following count
        var followingCount = await _unitOfWork.Follows.GetFollowingCountAsync(
            profile.Id,
            cancellationToken);

        return new OnboardingStatusResponse(
            IsCompleted: profile.OnboardingCompleted,
            HasHandle: !string.IsNullOrEmpty(profile.Handle),
            HasBio: !string.IsNullOrEmpty(profile.Bio),
            HasAvatar: !string.IsNullOrEmpty(profile.AvatarUrl),
            HasPostedPulse: pulseCount > 0,
            FollowingCount: followingCount);
    }
}
