using Codewrinkles.Domain.Nova;

namespace Codewrinkles.Application.Nova.Services;

/// <summary>
/// Service for semantic search over content chunks.
/// </summary>
public interface IContentSearchService
{
    /// <summary>
    /// Search content by semantic similarity.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="source">Optional source filter.</param>
    /// <param name="technology">Optional technology filter.</param>
    /// <param name="author">Optional author filter.</param>
    /// <param name="limit">Maximum results to return.</param>
    /// <param name="minSimilarity">Minimum similarity threshold.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<ContentSearchResult>> SearchAsync(
        string query,
        ContentSource? source = null,
        string? technology = null,
        string? author = null,
        int limit = 5,
        float minSimilarity = 0.7f,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a content search.
/// </summary>
public sealed record ContentSearchResult(
    Guid ChunkId,
    ContentSource Source,
    string? SourceUrl,
    string Title,
    string Content,
    string? Author,
    string? Technology,
    float Similarity);
