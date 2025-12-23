using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Application.Nova.Services;
using Codewrinkles.Domain.Nova;

namespace Codewrinkles.Infrastructure.Services.Nova;

/// <summary>
/// Implementation of content search using in-memory cached embeddings.
/// Embeddings are loaded at startup and kept in memory for fast similarity computation.
/// Can be migrated to Azure AI Search or SQL Server 2025 native vector when needed.
/// </summary>
public sealed class ContentSearchService : IContentSearchService
{
    private readonly ContentEmbeddingCache _cache;
    private readonly IEmbeddingService _embeddingService;

    public ContentSearchService(
        ContentEmbeddingCache cache,
        IEmbeddingService embeddingService)
    {
        _cache = cache;
        _embeddingService = embeddingService;
    }

    public async Task<IReadOnlyList<ContentSearchResult>> SearchAsync(
        string query,
        ContentSource? source = null,
        string? technology = null,
        string? author = null,
        int limit = 5,
        float minSimilarity = 0.7f,
        CancellationToken cancellationToken = default)
    {
        // Generate embedding for query
        var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query, cancellationToken);

        // Get all chunks from cache (pre-loaded at startup, embeddings pre-deserialized)
        var allChunks = await _cache.GetAllAsync(cancellationToken);

        // Filter by source/technology/author if specified
        var chunks = allChunks.AsEnumerable();

        if (source.HasValue)
        {
            chunks = chunks.Where(c => c.Source == source.Value);
        }

        if (!string.IsNullOrWhiteSpace(technology))
        {
            var tech = technology.ToLowerInvariant();
            chunks = chunks.Where(c => c.Technology == tech);
        }

        if (!string.IsNullOrWhiteSpace(author))
        {
            chunks = chunks.Where(c => c.Author == author);
        }

        // Calculate similarity and filter (embeddings already deserialized in cache)
        var scored = new List<(ContentSearchResult Result, float Similarity)>();

        foreach (var chunk in chunks)
        {
            var similarity = _embeddingService.CosineSimilarity(queryEmbedding, chunk.Embedding);

            if (similarity >= minSimilarity)
            {
                scored.Add((new ContentSearchResult(
                    chunk.Id,
                    chunk.Source,
                    chunk.Title,
                    chunk.Content,
                    chunk.Author,
                    chunk.Technology,
                    similarity), similarity));
            }
        }

        // Sort by similarity and take top N
        return scored
            .OrderByDescending(x => x.Similarity)
            .Take(limit)
            .Select(x => x.Result)
            .ToList();
    }
}
