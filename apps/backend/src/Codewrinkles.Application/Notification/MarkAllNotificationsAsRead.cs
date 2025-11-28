using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Notification;

public sealed record MarkAllNotificationsAsReadCommand(
    Guid UserId
) : ICommand<MarkAllNotificationsAsReadResult>;

public sealed record MarkAllNotificationsAsReadResult(
    bool Success
);

public sealed class MarkAllNotificationsAsReadCommandHandler
    : ICommandHandler<MarkAllNotificationsAsReadCommand, MarkAllNotificationsAsReadResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public MarkAllNotificationsAsReadCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MarkAllNotificationsAsReadResult> HandleAsync(
        MarkAllNotificationsAsReadCommand command,
        CancellationToken cancellationToken)
    {
        // Mark all unread notifications as read for this user
        await _unitOfWork.Notifications.MarkAllAsReadAsync(
            command.UserId,
            cancellationToken);

        return new MarkAllNotificationsAsReadResult(Success: true);
    }
}
