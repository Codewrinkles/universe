namespace Codewrinkles.Application.Common.Interfaces;

/// <summary>
/// Producer interface for queuing content ingestion jobs.
/// </summary>
public interface IContentIngestionQueue
{
    /// <summary>
    /// Queue a PDF for background processing.
    /// </summary>
    Task QueuePdfIngestionAsync(
        Guid jobId,
        byte[] pdfBytes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue a YouTube transcript for background processing.
    /// </summary>
    Task QueueTranscriptIngestionAsync(
        Guid jobId,
        string transcript,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue a documentation scraping job for background processing.
    /// </summary>
    Task QueueDocsScrapeAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue an article for background processing.
    /// </summary>
    Task QueueArticleIngestionAsync(
        Guid jobId,
        string content,
        CancellationToken cancellationToken = default);
}
