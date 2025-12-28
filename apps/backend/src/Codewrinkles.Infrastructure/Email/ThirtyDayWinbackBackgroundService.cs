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
        _logger.LogInformation(
            "30-day winback background service started. Scheduled: {Hour}:30 UTC",
            _settings.ReengagementHourUtc);

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
        // Runs at configured hour + 30 minutes (staggered from 7-day service)
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

            var novaEmails = 0;
            var codewrinklesEmails = 0;

            foreach (var candidate in candidates)
            {
                if (candidate.HasNovaAccess)
                {
                    await emailQueue.QueueThirtyDayNovaWinbackEmailAsync(
                        candidate.Email,
                        candidate.Name,
                        stoppingToken);
                    novaEmails++;
                }
                else
                {
                    await emailQueue.QueueThirtyDayCodewrinklesWinbackEmailAsync(
                        candidate.Email,
                        candidate.Name,
                        stoppingToken);
                    codewrinklesEmails++;
                }
            }

            _logger.LogInformation(
                "Queued {Total} 30-day winback emails ({Nova} Nova, {Codewrinkles} Codewrinkles)",
                novaEmails + codewrinklesEmails,
                novaEmails,
                codewrinklesEmails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during 30-day winback email run");
        }
    }
}
