using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// ReengagementCandidate is defined in Codewrinkles.Application.Common.Interfaces

namespace Codewrinkles.Infrastructure.Email;

/// <summary>
/// Background service that runs daily at a configured hour (default 4 AM UTC)
/// to find inactive users and queue re-engagement emails.
/// </summary>
public sealed class ReengagementBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EmailSettings _settings;
    private readonly ILogger<ReengagementBackgroundService> _logger;

    public ReengagementBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<EmailSettings> settings,
        ILogger<ReengagementBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Re-engagement background service started. Scheduled hour: {Hour} UTC",
            _settings.ReengagementHourUtc);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var nextRun = CalculateNextRunTime(now);
            var delay = nextRun - now;

            _logger.LogDebug(
                "Next re-engagement run scheduled for {NextRun} (in {Delay})",
                nextRun,
                delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunReengagementAsync(stoppingToken);
        }

        _logger.LogInformation("Re-engagement background service stopped");
    }

    internal DateTimeOffset CalculateNextRunTime(DateTimeOffset now)
    {
        // Calculate next occurrence of the configured hour
        var todayAtHour = new DateTimeOffset(
            now.Year, now.Month, now.Day,
            _settings.ReengagementHourUtc, 0, 0,
            TimeSpan.Zero);

        // If we've passed today's run time, schedule for tomorrow
        return now >= todayAtHour
            ? todayAtHour.AddDays(1)
            : todayAtHour;
    }

    private async Task RunReengagementAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting re-engagement email run");

        try
        {
            using var scope = _scopeFactory.CreateScope();

            var repository = scope.ServiceProvider.GetRequiredService<IReengagementRepository>();
            var emailQueue = scope.ServiceProvider.GetRequiredService<IEmailQueue>();

            var now = DateTimeOffset.UtcNow;
            var windowEnd = now.AddHours(-24);    // Inactive for at least 24 hours

            // Determine the window start based on whether win-back campaign is enabled.
            // Win-back mode: Catch ALL users inactive for more than 24 hours (no upper limit).
            // Normal mode: Only catch users in the 24-48 hour window.
            DateTimeOffset windowStart;

            if (_settings.WinbackCampaignEnabled)
            {
                // Win-back campaign: Include all users inactive for more than 24 hours (no upper bound)
                windowStart = DateTimeOffset.MinValue;
                _logger.LogInformation(
                    "Win-back campaign ENABLED - targeting ALL users inactive for more than 24 hours");
            }
            else
            {
                // Normal operation: 24-48 hour window
                windowStart = now.AddHours(-48);
                _logger.LogInformation(
                    "Normal mode - targeting users in 24-48 hour inactivity window");
            }

            var candidates = await repository.GetCandidatesAsync(
                windowStart,
                windowEnd,
                _settings.ReengagementBatchSize,
                stoppingToken);

            _logger.LogInformation(
                "Found {Count} re-engagement candidates",
                candidates.Count);

            var notificationEmails = 0;
            var feedUpdateEmails = 0;

            foreach (var candidate in candidates)
            {
                if (candidate.HasNotifications)
                {
                    // Priority: notification reminder email
                    await emailQueue.QueueNotificationReminderEmailAsync(
                        candidate.Email,
                        candidate.Name,
                        candidate.UnreadNotificationCount,
                        stoppingToken);
                    notificationEmails++;
                }
                else if (candidate.HasFeedUpdates)
                {
                    // Fallback: feed update email
                    await emailQueue.QueueFeedUpdateEmailAsync(
                        candidate.Email,
                        candidate.Name,
                        candidate.NewPulsesFromFollowsCount,
                        stoppingToken);
                    feedUpdateEmails++;
                }
                // else: shouldn't happen due to query filter, but skip if neither
            }

            _logger.LogInformation(
                "Queued {Total} re-engagement emails ({Notifications} notification reminders, {FeedUpdates} feed updates)",
                notificationEmails + feedUpdateEmails,
                notificationEmails,
                feedUpdateEmails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during re-engagement email run");
        }
    }
}
