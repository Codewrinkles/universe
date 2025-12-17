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

    /// <summary>
    /// Queue a 7-day winback email for an inactive user.
    /// Non-blocking - returns immediately.
    /// </summary>
    ValueTask QueueSevenDayWinbackEmailAsync(
        string toEmail,
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue a 30-day winback email for an inactive user.
    /// Non-blocking - returns immediately.
    /// </summary>
    ValueTask QueueThirtyDayWinbackEmailAsync(
        string toEmail,
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue an Alpha acceptance email with invite code.
    /// Non-blocking - returns immediately.
    /// </summary>
    ValueTask QueueAlphaAcceptanceEmailAsync(
        string toEmail,
        string userName,
        string inviteCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue an Alpha waitlist email.
    /// Non-blocking - returns immediately.
    /// </summary>
    ValueTask QueueAlphaWaitlistEmailAsync(
        string toEmail,
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue a congratulations email for users who earned Alpha access through Pulse activity.
    /// Non-blocking - returns immediately.
    /// </summary>
    ValueTask QueuePulseAlphaEarnedEmailAsync(
        string toEmail,
        string userName,
        int pulseCount,
        CancellationToken cancellationToken = default);
}
