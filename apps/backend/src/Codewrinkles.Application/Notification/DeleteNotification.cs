using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Telemetry;

namespace Codewrinkles.Application.Notification;

public sealed record DeleteNotificationCommand(
    Guid NotificationId,
    Guid UserId
) : ICommand<DeleteNotificationResult>;

public sealed record DeleteNotificationResult(
    bool Success
);

public sealed class DeleteNotificationCommandHandler
    : ICommandHandler<DeleteNotificationCommand, DeleteNotificationResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteNotificationCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteNotificationResult> HandleAsync(
        DeleteNotificationCommand command,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Notification.Delete);
        activity?.SetProfileId(command.UserId);
        activity?.SetTag(TagNames.Notification.Id, command.NotificationId.ToString());

        try
        {
            // Validator has already confirmed:
            // - Notification exists
            // - User is the recipient
            // So we can safely fetch and delete

            var notification = await _unitOfWork.Notifications.FindByIdAsync(command.NotificationId, cancellationToken);

            // Notification is guaranteed to exist (validator checked)
            _unitOfWork.Notifications.Delete(notification!);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            activity?.SetSuccess(true);

            return new DeleteNotificationResult(Success: true);
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}
