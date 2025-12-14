using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Codewrinkles.Infrastructure.Email;

/// <summary>
/// Background service that processes the email queue and sends via Resend.
///
/// Uses IServiceScopeFactory to create a scope per email, properly resolving
/// scoped dependencies (IEmailSender -> ResendClient -> HttpClient).
/// This follows Microsoft's recommended pattern for background services.
///
/// Rate limiting: Resend allows max 2 requests/second. We add a 600ms delay
/// between emails to stay safely under this limit.
/// </summary>
public sealed class EmailSenderBackgroundService : BackgroundService
{
    // Resend rate limit: 2 requests/second = 500ms minimum between requests
    // We use 600ms to have a safety buffer
    private static readonly TimeSpan RateLimitDelay = TimeSpan.FromMilliseconds(600);

    private readonly EmailChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailSenderBackgroundService> _logger;

    public EmailSenderBackgroundService(
        EmailChannel channel,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailSenderBackgroundService> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email sender background service started");

        await foreach (var message in _channel.ReadAllAsync(stoppingToken))
        {
            try
            {
                // Create a scope per email - ensures proper lifetime management
                // for scoped services (ResendClient, HttpClient, IOptionsSnapshot)
                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                await sender.SendAsync(message, stoppingToken);

                // Rate limiting: Resend allows max 2 requests/second
                // Delay between emails to avoid hitting the rate limit
                await Task.Delay(RateLimitDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown - exit the loop
                break;
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
