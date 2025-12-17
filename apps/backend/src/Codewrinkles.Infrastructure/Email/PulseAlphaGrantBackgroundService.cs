using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Codewrinkles.Infrastructure.Email;

/// <summary>
/// Background service that runs daily to find users with 15+ pulses in the last 30 days
/// who don't have Nova access, grants them Alpha access, and sends a congratulations email.
/// </summary>
public sealed class PulseAlphaGrantBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EmailSettings _settings;
    private readonly ILogger<PulseAlphaGrantBackgroundService> _logger;

    // Configuration for the gamification threshold
    private const int MinPulseCount = 15;
    private const int WindowDays = 30;

    public PulseAlphaGrantBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<EmailSettings> settings,
        ILogger<PulseAlphaGrantBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Pulse Alpha grant background service started. Scheduled: {Hour}:45 UTC. Threshold: {MinPulses}+ pulses in {Days} days",
            _settings.ReengagementHourUtc,
            MinPulseCount,
            WindowDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var nextRun = CalculateNextRunTime(now);
            var delay = nextRun - now;

            _logger.LogDebug(
                "Next Pulse Alpha grant run scheduled for {NextRun} (in {Delay})",
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

            await RunAlphaGrantAsync(stoppingToken);
        }

        _logger.LogInformation("Pulse Alpha grant background service stopped");
    }

    internal DateTimeOffset CalculateNextRunTime(DateTimeOffset now)
    {
        // Runs at configured hour + 45 minutes (staggered from other services)
        var todayAtTime = new DateTimeOffset(
            now.Year, now.Month, now.Day,
            _settings.ReengagementHourUtc, 45, 0,
            TimeSpan.Zero);

        return now >= todayAtTime
            ? todayAtTime.AddDays(1)
            : todayAtTime;
    }

    private async Task RunAlphaGrantAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Starting Pulse Alpha grant run. Looking for users with {MinPulses}+ pulses in {Days} days",
            MinPulseCount,
            WindowDays);

        try
        {
            using var scope = _scopeFactory.CreateScope();

            var repository = scope.ServiceProvider.GetRequiredService<IPulseAlphaGrantRepository>();
            var emailQueue = scope.ServiceProvider.GetRequiredService<IEmailQueue>();

            var candidates = await repository.GetCandidatesAsync(
                MinPulseCount,
                WindowDays,
                _settings.ReengagementBatchSize,
                stoppingToken);

            _logger.LogInformation(
                "Found {Count} users qualifying for automatic Nova Alpha access",
                candidates.Count);

            var grantedCount = 0;

            foreach (var candidate in candidates)
            {
                try
                {
                    // Grant Alpha access
                    await repository.GrantAlphaAccessAsync(candidate.ProfileId, stoppingToken);

                    // Queue congratulations email
                    await emailQueue.QueuePulseAlphaEarnedEmailAsync(
                        candidate.Email,
                        candidate.Name,
                        candidate.PulseCount,
                        stoppingToken);

                    grantedCount++;

                    _logger.LogInformation(
                        "Granted Nova Alpha access to {Name} ({Email}) - {PulseCount} pulses in last {Days} days",
                        candidate.Name,
                        candidate.Email,
                        candidate.PulseCount,
                        WindowDays);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error granting Alpha access to profile {ProfileId}",
                        candidate.ProfileId);
                }
            }

            _logger.LogInformation(
                "Pulse Alpha grant run complete. Granted access to {Count} users",
                grantedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Pulse Alpha grant run");
        }
    }
}
