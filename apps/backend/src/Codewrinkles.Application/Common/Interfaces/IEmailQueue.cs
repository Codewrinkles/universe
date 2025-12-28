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
    /// Queue a 7-day winback email for an inactive Nova user.
    /// Non-blocking - returns immediately.
    /// </summary>
    ValueTask QueueSevenDayNovaWinbackEmailAsync(
        string toEmail,
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue a 30-day winback email for an inactive Nova user.
    /// Includes warning about potential access revocation.
    /// Non-blocking - returns immediately.
    /// </summary>
    ValueTask QueueThirtyDayNovaWinbackEmailAsync(
        string toEmail,
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue a 7-day winback email for an inactive non-Nova user.
    /// Includes info about what they're missing (Pulse + Nova).
    /// Non-blocking - returns immediately.
    /// </summary>
    ValueTask QueueSevenDayCodewrinklesWinbackEmailAsync(
        string toEmail,
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue a 30-day winback email for an inactive non-Nova user.
    /// Includes info about what they're missing and how to get Nova access.
    /// Non-blocking - returns immediately.
    /// </summary>
    ValueTask QueueThirtyDayCodewrinklesWinbackEmailAsync(
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
