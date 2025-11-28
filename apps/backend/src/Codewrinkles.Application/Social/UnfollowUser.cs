using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Social;

public sealed record UnfollowUserCommand(
    Guid FollowerId,
    Guid FollowingId
) : ICommand<UnfollowResult>;

public sealed class UnfollowUserCommandHandler
    : ICommandHandler<UnfollowUserCommand, UnfollowResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public UnfollowUserCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UnfollowResult> HandleAsync(
        UnfollowUserCommand command,
        CancellationToken cancellationToken)
    {
        // Validator has already confirmed:
        // - Both profiles exist
        // - Follow relationship exists

        // 1. Fetch the follow relationship (guaranteed to exist after validation)
        var follow = (await _unitOfWork.Follows.FindFollowAsync(
            command.FollowerId,
            command.FollowingId,
            cancellationToken))!;

        // 2. Delete the follow
        _unitOfWork.Follows.DeleteFollow(follow);

        // 3. Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UnfollowResult(Success: true);
    }
}
