using Codewrinkles.Application.Nova;
using Kommand.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Codewrinkles.Infrastructure.Services.Nova;

/// <summary>
/// Background service that consumes memory extraction jobs from the channel.
/// Extracts insights from conversation sessions and creates memories for future context.
/// </summary>
public sealed class MemoryExtractionBackgroundService : BackgroundService
{
    private readonly MemoryExtractionChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MemoryExtractionBackgroundService> _logger;

    public MemoryExtractionBackgroundService(
        MemoryExtractionChannel channel,
        IServiceScopeFactory scopeFactory,
        ILogger<MemoryExtractionBackgroundService> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Memory extraction background service started");

        await foreach (var message in _channel.ReadAllAsync(stoppingToken))
        {
            try
            {
                // Create a scope per job - ensures proper lifetime for scoped services
                // Must use CreateAsyncScope() because UnitOfWork implements IAsyncDisposable
                await using var scope = _scopeFactory.CreateAsyncScope();

                await ProcessMessageAsync(message, scope.ServiceProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing memory extraction for profile {ProfileId}", message.ProfileId);
                // Log and continue - never crash the service
            }
        }

        _logger.LogInformation("Memory extraction background service stopped");
    }

    private async Task ProcessMessageAsync(
        MemoryExtractionMessage message,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var mediator = services.GetRequiredService<IMediator>();

        _logger.LogDebug("Processing memory extraction for profile {ProfileId}", message.ProfileId);

        var command = new TriggerMemoryExtractionCommand(message.ProfileId);
        var result = await mediator.SendAsync(command, cancellationToken);

        if (result.SessionsProcessed > 0)
        {
            _logger.LogInformation(
                "Completed memory extraction for profile {ProfileId}: {Sessions} sessions, {Memories} memories",
                message.ProfileId,
                result.SessionsProcessed,
                result.TotalMemoriesCreated);
        }
        else
        {
            _logger.LogDebug(
                "No sessions to process for profile {ProfileId}",
                message.ProfileId);
        }
    }
}
