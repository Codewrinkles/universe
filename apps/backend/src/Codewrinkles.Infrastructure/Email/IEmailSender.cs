namespace Codewrinkles.Infrastructure.Email;

/// <summary>
/// Abstraction for sending emails, allowing the background service
/// to be decoupled from specific email provider implementations.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends an email asynchronously.
    /// </summary>
    /// <param name="message">The email message to send.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if sent successfully, false otherwise.</returns>
    Task<bool> SendAsync(QueuedEmail message, CancellationToken ct = default);
}
