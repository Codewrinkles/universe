using Codewrinkles.Domain.Nova;

namespace Codewrinkles.Application.Common.Interfaces;

/// <summary>
/// Repository for content chunks used in RAG.
/// </summary>
public interface IContentChunkRepository
{
    // Query operations

    /// <summary>
    /// Find chunk by ID.
    /// </summary>
    Task<ContentChunk?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all chunks with embeddings for a specific source.
    /// </summary>
    Task<IReadOnlyList<ContentChunk>> GetBySourceAsync(
        ContentSource source,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all chunks with embeddings filtered by source and optional filters.
    /// Used for semantic search in application layer.
    /// </summary>
    Task<IReadOnlyList<ContentChunk>> GetWithEmbeddingsAsync(
        ContentSource? source = null,
        string? technology = null,
        string? author = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a chunk already exists for deduplication.
    /// </summary>
    Task<bool> ExistsBySourceIdentifierAsync(
        ContentSource source,
        string sourceIdentifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get chunk by source identifier for updates.
    /// </summary>
    Task<ContentChunk?> FindBySourceIdentifierAsync(
        ContentSource source,
        string sourceIdentifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all chunks for a parent document (multi-chunk sources).
    /// </summary>
    Task<IReadOnlyList<ContentChunk>> GetByParentDocumentIdAsync(
        string parentDocumentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the total count of chunks.
    /// </summary>
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the count of chunks by source.
    /// </summary>
    Task<int> GetCountBySourceAsync(
        ContentSource source,
        CancellationToken cancellationToken = default);

    // Write operations

    void Create(ContentChunk chunk);
    void Update(ContentChunk chunk);
    void Delete(ContentChunk chunk);

    /// <summary>
    /// Delete all chunks for a parent document (for re-ingestion).
    /// </summary>
    Task DeleteByParentDocumentIdAsync(
        string parentDocumentId,
        CancellationToken cancellationToken = default);
}
