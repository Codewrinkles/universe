using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Social;
using Codewrinkles.Telemetry;

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
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Social.Follow);
        activity?.SetProfileId(command.FollowerId);
        activity?.SetTag(TagNames.Social.TargetProfileId, command.FollowingId.ToString());

        try
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

            AppMetrics.RecordFollowCreated();
            AppMetrics.RecordNotificationCreated("follow");
            activity?.SetSuccess(true);

            return new FollowResult(Success: true);
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}
