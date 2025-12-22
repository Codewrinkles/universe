namespace Codewrinkles.Domain.Nova;

/// <summary>
/// Status of a content ingestion job.
/// </summary>
public enum IngestionJobStatus
{
    /// <summary>
    /// Job is queued and waiting to be processed.
    /// </summary>
    Queued = 0,

    /// <summary>
    /// Job is currently being processed.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Job completed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Job failed with an error.
    /// </summary>
    Failed = 3
}
