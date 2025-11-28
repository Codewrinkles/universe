using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

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
            throw new InvalidOperationException($"Notification with ID '{command.NotificationId}' not found.");
        }

        // Verify user owns this notification
        if (notification.RecipientId != command.UserId)
        {
            throw new InvalidOperationException("Cannot mark another user's notification as read.");
        }

        // Mark as read
        notification.MarkAsRead();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MarkNotificationAsReadResult(Success: true);
    }
}
