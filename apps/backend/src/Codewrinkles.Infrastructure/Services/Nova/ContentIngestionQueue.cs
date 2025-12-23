using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Infrastructure.Services.Nova;

/// <summary>
/// Producer implementation that writes to the ingestion channel.
/// </summary>
public sealed class ContentIngestionQueue : IContentIngestionQueue
{
    private readonly ContentIngestionChannel _channel;

    public ContentIngestionQueue(ContentIngestionChannel channel)
    {
        _channel = channel;
    }

    public async Task QueuePdfIngestionAsync(
        Guid jobId,
        byte[] pdfBytes,
        CancellationToken cancellationToken = default)
    {
        var message = new PdfIngestionMessage(jobId, pdfBytes);
        await _channel.WriteAsync(message, cancellationToken);
    }

    public async Task QueueTranscriptIngestionAsync(
        Guid jobId,
        string transcript,
        CancellationToken cancellationToken = default)
    {
        var message = new TranscriptIngestionMessage(jobId, transcript);
        await _channel.WriteAsync(message, cancellationToken);
    }

    public async Task QueueDocsScrapeAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var message = new DocsScrapeMessage(jobId);
        await _channel.WriteAsync(message, cancellationToken);
    }

    public async Task QueueArticleIngestionAsync(
        Guid jobId,
        string content,
        CancellationToken cancellationToken = default)
    {
        var message = new ArticleIngestionMessage(jobId, content);
        await _channel.WriteAsync(message, cancellationToken);
    }
}
