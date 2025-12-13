using Codewrinkles.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;

namespace Codewrinkles.Infrastructure.Email;

public sealed class ResendEmailSender : IEmailSender
{
    private readonly ResendClient _resend;
    private readonly EmailSettings _settings;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(
        ResendClient resend,
        IOptions<EmailSettings> settings,
        ILogger<ResendEmailSender> logger)
    {
        _resend = resend;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> SendAsync(QueuedEmail message, CancellationToken ct = default)
    {
        try
        {
            var email = new EmailMessage
            {
                From = $"{_settings.FromName} <{_settings.FromAddress}>",
                Subject = message.Subject,
                HtmlBody = message.HtmlBody
            };

            // To is a collection - add the recipient
            var toAddress = string.IsNullOrWhiteSpace(message.ToName)
                ? message.ToEmail
                : $"{message.ToName} <{message.ToEmail}>";
            email.To.Add(toAddress);

            await _resend.EmailSendAsync(email, ct);

            _logger.LogInformation(
                "Email sent: {Subject} to {Email}",
                message.Subject,
                message.ToEmail);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email: {Subject} to {Email}",
                message.Subject,
                message.ToEmail);

            return false;
        }
    }
}
