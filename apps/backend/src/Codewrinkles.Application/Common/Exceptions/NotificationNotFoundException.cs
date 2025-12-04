namespace Codewrinkles.Application.Common.Exceptions;

/// <summary>
/// Thrown when attempting to access a notification that does not exist.
/// </summary>
public sealed class NotificationNotFoundException : Exception
{
    public NotificationNotFoundException(Guid notificationId)
        : base($"Notification with ID '{notificationId}' was not found.")
    {
        NotificationId = notificationId;
    }

    public Guid NotificationId { get; }
}
