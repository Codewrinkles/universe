namespace Codewrinkles.Application.Common.Exceptions;

/// <summary>
/// Thrown when a user attempts to access or modify a notification that belongs to another user.
/// </summary>
public sealed class UnauthorizedNotificationAccessException : Exception
{
    public UnauthorizedNotificationAccessException(Guid notificationId, Guid requestingUserId)
        : base($"User '{requestingUserId}' is not authorized to access notification '{notificationId}'.")
    {
        NotificationId = notificationId;
        RequestingUserId = requestingUserId;
    }

    public Guid NotificationId { get; }
    public Guid RequestingUserId { get; }
}
