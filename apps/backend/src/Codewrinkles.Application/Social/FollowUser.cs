using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Social;

namespace Codewrinkles.Application.Social;

public sealed record FollowUserCommand(
    Guid FollowerId,
    Guid FollowingId
) : ICommand<FollowResult>;

public sealed class FollowUserCommandHandler
    : ICommandHandler<FollowUserCommand, FollowResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public FollowUserCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<FollowResult> HandleAsync(
        FollowUserCommand command,
        CancellationToken cancellationToken)
    {
        // Validator has already confirmed:
        // - Both profiles exist
        // - Not attempting to follow self
        // - Not already following

        // 1. Create Follow entity via factory method
        var follow = Follow.Create(command.FollowerId, command.FollowingId);

        // 2. Add to repository
        _unitOfWork.Follows.CreateFollow(follow);

        // 3. Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Create follow notification
        var notification = Domain.Notification.Notification.CreateFollowNotification(
            recipientId: command.FollowingId,
            actorId: command.FollowerId);
        _unitOfWork.Notifications.Create(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new FollowResult(Success: true);
    }
}
