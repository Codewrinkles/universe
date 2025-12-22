using Codewrinkles.Domain.Nova;

namespace Codewrinkles.Application.Common.Interfaces;

/// <summary>
/// Repository for content ingestion jobs.
/// </summary>
public interface IContentIngestionJobRepository
{
    /// <summary>
    /// Find job by ID.
    /// </summary>
    Task<ContentIngestionJob?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find job by parent document ID.
    /// </summary>
    Task<ContentIngestionJob?> FindByParentDocumentIdAsync(
        string parentDocumentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all jobs with optional status filter.
    /// </summary>
    Task<IReadOnlyList<ContentIngestionJob>> GetAllAsync(
        IngestionJobStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get jobs that are queued and ready for processing.
    /// </summary>
    Task<IReadOnlyList<ContentIngestionJob>> GetQueuedJobsAsync(
        int limit = 10,
        CancellationToken cancellationToken = default);

    void Create(ContentIngestionJob job);
    void Update(ContentIngestionJob job);
    void Delete(ContentIngestionJob job);
}
