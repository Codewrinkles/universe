using Codewrinkles.Application.Common.Exceptions;
using Codewrinkles.Application.Common.Interfaces;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Notification;

public sealed record MarkNotificationAsReadCommand(
    Guid NotificationId,
    Guid UserId
) : ICommand<MarkNotificationAsReadResult>;

public sealed record MarkNotificationAsReadResult(
    bool Success
);

public sealed class MarkNotificationAsReadCommandHandler
    : ICommandHandler<MarkNotificationAsReadCommand, MarkNotificationAsReadResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public MarkNotificationAsReadCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MarkNotificationAsReadResult> HandleAsync(
        MarkNotificationAsReadCommand command,
        CancellationToken cancellationToken)
    {
        // Get notification with tracking
        var notification = await _unitOfWork.Notifications.FindByIdAsync(
            command.NotificationId,
            cancellationToken);

        if (notification is null)
        {
            throw new NotificationNotFoundException(command.NotificationId);
        }

        // Verify user owns this notification
        if (notification.RecipientId != command.UserId)
        {
            throw new UnauthorizedNotificationAccessException(command.NotificationId, command.UserId);
        }

        // Mark as read
        notification.MarkAsRead();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MarkNotificationAsReadResult(Success: true);
    }
}
