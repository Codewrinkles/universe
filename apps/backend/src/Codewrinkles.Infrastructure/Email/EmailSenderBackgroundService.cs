using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Codewrinkles.Infrastructure.Email;

/// <summary>
/// Background service that processes the email queue and sends via Resend.
/// </summary>
public sealed class EmailSenderBackgroundService : BackgroundService
{
    private readonly EmailChannel _channel;
    private readonly ResendEmailSender _sender;
    private readonly ILogger<EmailSenderBackgroundService> _logger;

    public EmailSenderBackgroundService(
        EmailChannel channel,
        ResendEmailSender sender,
        ILogger<EmailSenderBackgroundService> logger)
    {
        _channel = channel;
        _sender = sender;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email sender background service started");

        await foreach (var message in _channel.ReadAllAsync(stoppingToken))
        {
            try
            {
                await _sender.SendAsync(message, stoppingToken);
            }
            catch (Exception ex)
            {
                // Log and continue - never crash the service
                _logger.LogError(
                    ex,
                    "Unexpected error processing email to {Email}",
                    message.ToEmail);
            }
        }

        _logger.LogInformation("Email sender background service stopped");
    }
}
