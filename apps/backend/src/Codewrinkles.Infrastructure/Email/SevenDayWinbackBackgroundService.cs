using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Codewrinkles.Infrastructure.Email;

/// <summary>
/// Background service that runs daily at configured hour + 30 minutes
/// to find users inactive for 6-7 days and queue winback emails.
/// </summary>
public sealed class SevenDayWinbackBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EmailSettings _settings;
    private readonly ILogger<SevenDayWinbackBackgroundService> _logger;

    public SevenDayWinbackBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<EmailSettings> settings,
        ILogger<SevenDayWinbackBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "7-day winback background service started. Scheduled: {Hour}:30 UTC",
            _settings.ReengagementHourUtc);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var nextRun = CalculateNextRunTime(now);
            var delay = nextRun - now;

            _logger.LogDebug(
                "Next 7-day winback run scheduled for {NextRun} (in {Delay})",
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

        _logger.LogInformation("7-day winback background service stopped");
    }

    internal DateTimeOffset CalculateNextRunTime(DateTimeOffset now)
    {
        // Runs at configured hour + 30 minutes (staggered from 24h service)
        var todayAtTime = new DateTimeOffset(
            now.Year, now.Month, now.Day,
            _settings.ReengagementHourUtc, 30, 0,
            TimeSpan.Zero);

        return now >= todayAtTime
            ? todayAtTime.AddDays(1)
            : todayAtTime;
    }

    private async Task RunWinbackAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting 7-day winback email run");

        try
        {
            using var scope = _scopeFactory.CreateScope();

            var repository = scope.ServiceProvider.GetRequiredService<IReengagementRepository>();
            var emailQueue = scope.ServiceProvider.GetRequiredService<IEmailQueue>();

            var now = DateTimeOffset.UtcNow;
            // Window: 6-7 days ago
            var windowStart = now.AddDays(-7);
            var windowEnd = now.AddDays(-6);

            var candidates = await repository.GetWinbackCandidatesAsync(
                windowStart,
                windowEnd,
                _settings.ReengagementBatchSize,
                stoppingToken);

            _logger.LogInformation(
                "Found {Count} 7-day winback candidates",
                candidates.Count);

            foreach (var candidate in candidates)
            {
                await emailQueue.QueueSevenDayWinbackEmailAsync(
                    candidate.Email,
                    candidate.Name,
                    stoppingToken);
            }

            _logger.LogInformation(
                "Queued {Count} 7-day winback emails",
                candidates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during 7-day winback email run");
        }
    }
}
