using Codewrinkles.Domain.Nova;

namespace Codewrinkles.Application.Common.Interfaces;

public interface INovaMemoryRepository
{
    // Query operations

    /// <summary>
    /// Find memory by ID.
    /// Returns null if not found.
    /// </summary>
    Task<Memory?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active (non-superseded) memories for a profile.
    /// </summary>
    Task<IReadOnlyList<Memory>> GetActiveByProfileIdAsync(
        Guid profileId,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active memories by category for a profile.
    /// </summary>
    Task<IReadOnlyList<Memory>> GetActiveByCategoryAsync(
        Guid profileId,
        MemoryCategory category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get most recent memories for a profile (by CreatedAt desc).
    /// </summary>
    Task<IReadOnlyList<Memory>> GetRecentAsync(
        Guid profileId,
        int count,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get memories with embeddings for semantic search.
    /// Only returns active memories that have embeddings set.
    /// </summary>
    Task<IReadOnlyList<Memory>> GetWithEmbeddingsAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get high-importance memories (importance >= threshold).
    /// Only returns active memories.
    /// </summary>
    Task<IReadOnlyList<Memory>> GetByMinImportanceAsync(
        Guid profileId,
        int minImportance,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Find the active memory for a single-cardinality category.
    /// Returns null if no active memory exists.
    /// </summary>
    Task<Memory?> FindActiveByCategoryAsync(
        Guid profileId,
        MemoryCategory category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Find similar memories by content (for deduplication).
    /// Uses exact content match - semantic similarity done in app layer.
    /// </summary>
    Task<Memory?> FindByContentAsync(
        Guid profileId,
        MemoryCategory category,
        string content,
        CancellationToken cancellationToken = default);

    // Write operations

    /// <summary>
    /// Create a new memory.
    /// Does not save to database - call UnitOfWork.SaveChangesAsync().
    /// </summary>
    void Create(Memory memory);

    /// <summary>
    /// Update a memory.
    /// Does not save to database - call UnitOfWork.SaveChangesAsync().
    /// </summary>
    void Update(Memory memory);
}
