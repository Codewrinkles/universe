using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Codewrinkles.Infrastructure.Services.Nova;

/// <summary>
/// Singleton cache for content chunk embeddings.
/// Eliminates database round-trips for RAG search by keeping all embeddings in memory.
/// Embeddings are pre-deserialized for fast similarity computation.
/// </summary>
public sealed class ContentEmbeddingCache
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ContentEmbeddingCache> _logger;

    // Cached data - null until first refresh
    private IReadOnlyList<CachedChunk>? _chunks;

    // Synchronization
    private readonly TaskCompletionSource _initialized = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public ContentEmbeddingCache(
        IServiceScopeFactory scopeFactory,
        ILogger<ContentEmbeddingCache> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets all cached chunks. Waits if cache is not yet initialized.
    /// </summary>
    public async Task<IReadOnlyList<CachedChunk>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // Wait for initialization to complete (no-op if already done)
        await _initialized.Task.WaitAsync(cancellationToken);
        return _chunks!;
    }

    /// <summary>
    /// Gets the current chunk count. Returns 0 if not initialized.
    /// </summary>
    public int Count => _chunks?.Count ?? 0;

    /// <summary>
    /// Whether the cache has been initialized.
    /// </summary>
    public bool IsInitialized => _initialized.Task.IsCompleted;

    /// <summary>
    /// Refreshes the cache from the database.
    /// Called at startup by ContentEmbeddingCacheInitializer and after content ingestion.
    /// </summary>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogDebug("Loading content chunks from database...");

            await using var scope = _scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IContentChunkRepository>();
            var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();

            // Load all chunks from database
            var dbChunks = await repository.GetWithEmbeddingsAsync(cancellationToken: cancellationToken);

            // Transform to cached format with pre-deserialized embeddings
            var cachedChunks = new List<CachedChunk>(dbChunks.Count);

            foreach (var chunk in dbChunks)
            {
                var embedding = embeddingService.DeserializeEmbedding(chunk.Embedding);

                cachedChunks.Add(new CachedChunk(
                    chunk.Id,
                    chunk.Source,
                    chunk.Title,
                    chunk.Content,
                    chunk.Author,
                    chunk.Technology,
                    embedding));
            }

            // Update cache atomically
            _chunks = cachedChunks;

            // Signal initialization complete (only matters on first call)
            _initialized.TrySetResult();

            _logger.LogDebug(
                "Content embedding cache refreshed: {Count} chunks, ~{SizeMB:F1} MB",
                cachedChunks.Count,
                EstimateMemoryMB(cachedChunks));
        }
        catch (Exception ex)
        {
            // If this is the first initialization attempt, propagate the failure
            _initialized.TrySetException(ex);
            throw;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static double EstimateMemoryMB(List<CachedChunk> chunks)
    {
        if (chunks.Count == 0) return 0;

        // Estimate: embeddings (1536 floats × 4 bytes) + content (~4KB avg) + metadata (~200 bytes)
        const int embeddingBytes = 1536 * 4;
        const int avgContentBytes = 4000;
        const int metadataBytes = 200;
        const int bytesPerChunk = embeddingBytes + avgContentBytes + metadataBytes;

        return chunks.Count * bytesPerChunk / (1024.0 * 1024.0);
    }
}

/// <summary>
/// Cached representation of a content chunk with pre-deserialized embedding.
/// </summary>
public sealed record CachedChunk(
    Guid Id,
    ContentSource Source,
    string Title,
    string Content,
    string? Author,
    string? Technology,
    float[] Embedding);
