using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Codewrinkles.Infrastructure.Services.Nova;

/// <summary>
/// Hosted service that initializes the content embedding cache at application startup.
/// Ensures the cache is populated before the application starts accepting requests.
/// </summary>
public sealed class ContentEmbeddingCacheInitializer : IHostedService
{
    private readonly ContentEmbeddingCache _cache;
    private readonly ILogger<ContentEmbeddingCacheInitializer> _logger;

    public ContentEmbeddingCacheInitializer(
        ContentEmbeddingCache cache,
        ILogger<ContentEmbeddingCacheInitializer> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing content embedding cache...");

        var sw = Stopwatch.StartNew();

        try
        {
            await _cache.RefreshAsync(cancellationToken);

            _logger.LogInformation(
                "Content embedding cache initialized: {Count} chunks loaded in {Elapsed}ms",
                _cache.Count,
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to initialize content embedding cache after {Elapsed}ms",
                sw.ElapsedMilliseconds);

            // Re-throw to fail startup - we don't want to run without the cache
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
