namespace Codewrinkles.Infrastructure.Email;

/// <summary>
/// Represents an email message queued for background sending.
/// Named QueuedEmail to avoid conflict with Resend.EmailMessage.
/// </summary>
public sealed record QueuedEmail(
    string ToEmail,
    string ToName,
    string Subject,
    string HtmlBody);
