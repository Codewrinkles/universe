using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Codewrinkles.Infrastructure.Email;

/// <summary>
/// Background service that runs daily at configured hour + 1 hour
/// to find users inactive for 29-30 days and queue winback emails.
/// </summary>
public sealed class ThirtyDayWinbackBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EmailSettings _settings;
    private readonly ILogger<ThirtyDayWinbackBackgroundService> _logger;

    public ThirtyDayWinbackBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<EmailSettings> settings,
        ILogger<ThirtyDayWinbackBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var targetHour = (_settings.ReengagementHourUtc + 1) % 24;
        _logger.LogInformation(
            "30-day winback background service started. Scheduled: {Hour}:00 UTC",
            targetHour);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var nextRun = CalculateNextRunTime(now);
            var delay = nextRun - now;

            _logger.LogDebug(
                "Next 30-day winback run scheduled for {NextRun} (in {Delay})",
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

            await RunWinbackAsync(stoppingToken);
        }

        _logger.LogInformation("30-day winback background service stopped");
    }

    internal DateTimeOffset CalculateNextRunTime(DateTimeOffset now)
    {
        // Runs 1 hour after base hour (staggered from 7-day service)
        // Handle edge case: if base hour is 23, target hour is 0 of next day
        var baseHour = _settings.ReengagementHourUtc;
        var targetHour = (baseHour + 1) % 24;
        var crossesMidnight = baseHour == 23;

        var todayAtTime = new DateTimeOffset(
            now.Year, now.Month, now.Day,
            targetHour, 0, 0,
            TimeSpan.Zero);

        // If base hour is 23, the +1 hour crosses midnight into the next day
        if (crossesMidnight)
        {
            todayAtTime = todayAtTime.AddDays(1);
        }

        return now >= todayAtTime
            ? todayAtTime.AddDays(1)
            : todayAtTime;
    }

    private async Task RunWinbackAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting 30-day winback email run");

        try
        {
            using var scope = _scopeFactory.CreateScope();

            var repository = scope.ServiceProvider.GetRequiredService<IReengagementRepository>();
            var emailQueue = scope.ServiceProvider.GetRequiredService<IEmailQueue>();

            var now = DateTimeOffset.UtcNow;
            // Window: 29-30 days ago
            var windowStart = now.AddDays(-30);
            var windowEnd = now.AddDays(-29);

            var candidates = await repository.GetWinbackCandidatesAsync(
                windowStart,
                windowEnd,
                _settings.ReengagementBatchSize,
                stoppingToken);

            _logger.LogInformation(
                "Found {Count} 30-day winback candidates",
                candidates.Count);

            foreach (var candidate in candidates)
            {
                await emailQueue.QueueThirtyDayWinbackEmailAsync(
                    candidate.Email,
                    candidate.Name,
                    stoppingToken);
            }

            _logger.LogInformation(
                "Queued {Count} 30-day winback emails",
                candidates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during 30-day winback email run");
        }
    }
}
