using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Social.Exceptions;
using Kommand;

namespace Codewrinkles.Application.Social;

public sealed class FollowUserValidator : IValidator<FollowUserCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private List<ValidationError> _errors = null!;

    public FollowUserValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ValidationResult> ValidateAsync(
        FollowUserCommand request,
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
        await ValidateFollowAsync(request, cancellationToken);

        return _errors.Count > 0
            ? ValidationResult.Failure(_errors)
            : ValidationResult.Success();
    }

    private void ValidateGuids(Guid followerId, Guid followingId)
    {
        if (followerId == Guid.Empty)
        {
            _errors.Add(new ValidationError(
                nameof(FollowUserCommand.FollowerId),
                "Follower ID is required"));
        }

        if (followingId == Guid.Empty)
        {
            _errors.Add(new ValidationError(
                nameof(FollowUserCommand.FollowingId),
                "Following ID is required"));
        }

        // Check for self-follow
        if (followerId == followingId)
        {
            throw new FollowSelfException("Cannot follow yourself");
        }
    }

    private async Task ValidateFollowAsync(
        FollowUserCommand request,
        CancellationToken cancellationToken)
    {
        // Check if follower profile exists
        var followerExists = await _unitOfWork.Profiles.FindByIdAsync(
            request.FollowerId,
            cancellationToken);

        if (followerExists is null)
        {
            _errors.Add(new ValidationError(
                nameof(FollowUserCommand.FollowerId),
                "Follower profile not found"));
        }

        // Check if following profile exists
        var followingExists = await _unitOfWork.Profiles.FindByIdAsync(
            request.FollowingId,
            cancellationToken);

        if (followingExists is null)
        {
            _errors.Add(new ValidationError(
                nameof(FollowUserCommand.FollowingId),
                "Profile to follow not found"));
        }

        // If profiles don't exist, return early
        if (_errors.Count > 0)
        {
            return;
        }

        // Check if already following
        var existingFollow = await _unitOfWork.Follows.FindFollowAsync(
            request.FollowerId,
            request.FollowingId,
            cancellationToken);

        if (existingFollow is not null)
        {
            throw new AlreadyFollowingException(request.FollowerId, request.FollowingId);
        }
    }
}
