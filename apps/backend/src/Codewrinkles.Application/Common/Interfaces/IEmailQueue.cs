namespace Codewrinkles.Application.Common.Interfaces;

public interface IEmailQueue
{
    /// <summary>
    /// Queue a welcome email for a newly registered user.
    /// Non-blocking - returns immediately.
    /// </summary>
    ValueTask QueueWelcomeEmailAsync(
        string toEmail,
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue a re-engagement email for an inactive user WITH notifications.
    /// Non-blocking - returns immediately.
    /// </summary>
    ValueTask QueueNotificationReminderEmailAsync(
        string toEmail,
        string userName,
        int unreadNotificationCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue a re-engagement email for an inactive user WITHOUT notifications
    /// but with new content in their feed.
    /// Non-blocking - returns immediately.
    /// </summary>
    ValueTask QueueFeedUpdateEmailAsync(
        string toEmail,
        string userName,
        int newPulsesCount,
        CancellationToken cancellationToken = default);
}
