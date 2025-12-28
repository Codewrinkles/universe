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

    public async ValueTask QueueSevenDayNovaWinbackEmailAsync(
        string toEmail,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var subject = "We miss you on Nova!";
        var htmlBody = EmailTemplates.BuildSevenDayNovaWinbackEmail(userName, _settings.BaseUrl);

        var message = new QueuedEmail(toEmail, userName, subject, htmlBody);
        await _channel.WriteAsync(message, cancellationToken);
    }

    public async ValueTask QueueThirtyDayNovaWinbackEmailAsync(
        string toEmail,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var subject = "Important: Your Nova Alpha access";
        var htmlBody = EmailTemplates.BuildThirtyDayNovaWinbackEmail(userName, _settings.BaseUrl);

        var message = new QueuedEmail(toEmail, userName, subject, htmlBody);
        await _channel.WriteAsync(message, cancellationToken);
    }

    public async ValueTask QueueSevenDayCodewrinklesWinbackEmailAsync(
        string toEmail,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var subject = "We miss you on Codewrinkles!";
        var htmlBody = EmailTemplates.BuildSevenDayCodewrinklesWinbackEmail(userName, _settings.BaseUrl);

        var message = new QueuedEmail(toEmail, userName, subject, htmlBody);
        await _channel.WriteAsync(message, cancellationToken);
    }

    public async ValueTask QueueThirtyDayCodewrinklesWinbackEmailAsync(
        string toEmail,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var subject = "Come back and discover Codewrinkles";
        var htmlBody = EmailTemplates.BuildThirtyDayCodewrinklesWinbackEmail(userName, _settings.BaseUrl);

        var message = new QueuedEmail(toEmail, userName, subject, htmlBody);
        await _channel.WriteAsync(message, cancellationToken);
    }

    public async ValueTask QueueAlphaAcceptanceEmailAsync(
        string toEmail,
        string userName,
        string inviteCode,
        CancellationToken cancellationToken = default)
    {
        var subject = "You're In! Welcome to Nova Alpha";
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

    public async ValueTask QueuePulseAlphaEarnedEmailAsync(
        string toEmail,
        string userName,
        int pulseCount,
        CancellationToken cancellationToken = default)
    {
        var subject = "You Earned Nova Alpha Access!";
        var htmlBody = EmailTemplates.BuildPulseAlphaEarnedEmail(userName, pulseCount, _settings.BaseUrl);

        var message = new QueuedEmail(toEmail, userName, subject, htmlBody);
        await _channel.WriteAsync(message, cancellationToken);
    }
}
