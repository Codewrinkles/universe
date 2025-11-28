using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Social.Exceptions;
using Kommand;

namespace Codewrinkles.Application.Social;

public sealed class UnfollowUserValidator : IValidator<UnfollowUserCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private List<ValidationError> _errors = null!;

    public UnfollowUserValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ValidationResult> ValidateAsync(
        UnfollowUserCommand request,
        CancellationToken cancellationToken)
    {
        _errors = [];

        // Input validation
        ValidateGuids(request.FollowerId, request.FollowingId);

        // If basic validation fails, return early
        if (_errors.Count > 0)
        {
            return ValidationResult.Failure(_errors);
        }

        // Application-level validation (requires database checks)
        await ValidateUnfollowAsync(request, cancellationToken);

        return _errors.Count > 0
            ? ValidationResult.Failure(_errors)
            : ValidationResult.Success();
    }

    private void ValidateGuids(Guid followerId, Guid followingId)
    {
        if (followerId == Guid.Empty)
        {
            _errors.Add(new ValidationError(
                nameof(UnfollowUserCommand.FollowerId),
                "Follower ID is required"));
        }

        if (followingId == Guid.Empty)
        {
            _errors.Add(new ValidationError(
                nameof(UnfollowUserCommand.FollowingId),
                "Following ID is required"));
        }
    }

    private async Task ValidateUnfollowAsync(
        UnfollowUserCommand request,
        CancellationToken cancellationToken)
    {
        // Check if follower profile exists
        var followerExists = await _unitOfWork.Profiles.FindByIdAsync(
            request.FollowerId,
            cancellationToken);

        if (followerExists is null)
        {
            _errors.Add(new ValidationError(
                nameof(UnfollowUserCommand.FollowerId),
                "Follower profile not found"));
        }

        // Check if following profile exists
        var followingExists = await _unitOfWork.Profiles.FindByIdAsync(
            request.FollowingId,
            cancellationToken);

        if (followingExists is null)
        {
            _errors.Add(new ValidationError(
                nameof(UnfollowUserCommand.FollowingId),
                "Profile not found"));
        }

        // If profiles don't exist, return early
        if (_errors.Count > 0)
        {
            return;
        }

        // Check if follow relationship exists
        var existingFollow = await _unitOfWork.Follows.FindFollowAsync(
            request.FollowerId,
            request.FollowingId,
            cancellationToken);

        if (existingFollow is null)
        {
            throw new NotFollowingException(request.FollowerId, request.FollowingId);
        }
    }
}
