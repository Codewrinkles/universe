namespace Codewrinkles.Application.Common.Interfaces;

public interface INotificationRepository
{
    /// <summary>
    /// Creates a new notification
    /// </summary>
    void Create(Domain.Notification.Notification notification);

    /// <summary>
    /// Finds a notification by ID with tracking enabled
    /// </summary>
    Task<Domain.Notification.Notification?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated notifications for a user, ordered by creation date descending
    /// Includes actor profile information
    /// </summary>
    Task<(List<Domain.Notification.Notification> Notifications, int TotalCount)> GetByRecipientAsync(
        Guid recipientId,
        int offset,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets count of unread notifications for a user
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid recipientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks all notifications for a user as read
    /// </summary>
    Task MarkAllAsReadAsync(Guid recipientId, CancellationToken cancellationToken = default);
}
