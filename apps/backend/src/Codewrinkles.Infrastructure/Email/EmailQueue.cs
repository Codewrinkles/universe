using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Codewrinkles.Infrastructure.Email;

public sealed class EmailQueue : IEmailQueue
{
    private readonly EmailChannel _channel;
    private readonly EmailSettings _settings;

    public EmailQueue(EmailChannel channel, IOptions<EmailSettings> settings)
    {
        _channel = channel;
        _settings = settings.Value;
    }

    public async ValueTask QueueWelcomeEmailAsync(
        string toEmail,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var subject = "Welcome to Codewrinkles!";
        var htmlBody = EmailTemplates.BuildWelcomeEmail(userName, _settings.BaseUrl);

        var message = new QueuedEmail(toEmail, userName, subject, htmlBody);
        await _channel.WriteAsync(message, cancellationToken);
    }

    public async ValueTask QueueNotificationReminderEmailAsync(
        string toEmail,
        string userName,
        int unreadNotificationCount,
        CancellationToken cancellationToken = default)
    {
        var subject = $"You have {unreadNotificationCount} unread notification{(unreadNotificationCount == 1 ? "" : "s")} on Pulse";
        var htmlBody = EmailTemplates.BuildNotificationReminderEmail(
            userName,
            unreadNotificationCount,
            _settings.BaseUrl);

        var message = new QueuedEmail(toEmail, userName, subject, htmlBody);
        await _channel.WriteAsync(message, cancellationToken);
    }

    public async ValueTask QueueFeedUpdateEmailAsync(
        string toEmail,
        string userName,
        int newPulsesCount,
        CancellationToken cancellationToken = default)
    {
        var subject = $"Your feed has {newPulsesCount} new pulse{(newPulsesCount == 1 ? "" : "s")}";
        var htmlBody = EmailTemplates.BuildFeedUpdateEmail(
            userName,
            newPulsesCount,
            _settings.BaseUrl);

        var message = new QueuedEmail(toEmail, userName, subject, htmlBody);
        await _channel.WriteAsync(message, cancellationToken);
    }

    public async ValueTask QueueSevenDayWinbackEmailAsync(
        string toEmail,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var subject = "We miss you on Pulse!";
        var htmlBody = EmailTemplates.BuildSevenDayWinbackEmail(userName, _settings.BaseUrl);

        var message = new QueuedEmail(toEmail, userName, subject, htmlBody);
        await _channel.WriteAsync(message, cancellationToken);
    }

    public async ValueTask QueueThirtyDayWinbackEmailAsync(
        string toEmail,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var subject = "It's been a while...";
        var htmlBody = EmailTemplates.BuildThirtyDayWinbackEmail(userName, _settings.BaseUrl);

        var message = new QueuedEmail(toEmail, userName, subject, htmlBody);
        await _channel.WriteAsync(message, cancellationToken);
    }

    public async ValueTask QueueAlphaAcceptanceEmailAsync(
        string toEmail,
        string userName,
        string inviteCode,
        CancellationToken cancellationToken = default)
    {
        var subject = "You're In! Welcome to Nova Alpha 🎉";
        var htmlBody = EmailTemplates.BuildAlphaAcceptanceEmail(userName, inviteCode, _settings.BaseUrl);

        var message = new QueuedEmail(toEmail, userName, subject, htmlBody);
        await _channel.WriteAsync(message, cancellationToken);
    }

    public async ValueTask QueueAlphaWaitlistEmailAsync(
        string toEmail,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var subject = "You're on the Nova Waitlist";
        var htmlBody = EmailTemplates.BuildAlphaWaitlistEmail(userName, _settings.BaseUrl);

        var message = new QueuedEmail(toEmail, userName, subject, htmlBody);
        await _channel.WriteAsync(message, cancellationToken);
    }
}
