using System.Threading.Channels;

namespace Codewrinkles.Infrastructure.Services.Nova;

/// <summary>
/// Singleton channel for content ingestion jobs.
/// Fire-and-forget pattern: producers write to channel, background service consumes.
/// </summary>
public sealed class ContentIngestionChannel
{
    private readonly Channel<ContentIngestionMessage> _channel;

    public ContentIngestionChannel()
    {
        // Unbounded channel - jobs queue up without blocking
        // For production, consider BoundedChannelOptions with backpressure
        _channel = Channel.CreateUnbounded<ContentIngestionMessage>(
            new UnboundedChannelOptions
            {
                SingleReader = true,  // Only one background service reads
                SingleWriter = false  // Multiple endpoints can queue
            });
    }

    public ValueTask WriteAsync(ContentIngestionMessage message, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(message, ct);

    public IAsyncEnumerable<ContentIngestionMessage> ReadAllAsync(CancellationToken ct = default)
        => _channel.Reader.ReadAllAsync(ct);
}

/// <summary>
/// Base type for ingestion channel messages.
/// </summary>
public abstract record ContentIngestionMessage(Guid JobId);

/// <summary>
/// Message for PDF ingestion jobs.
/// </summary>
public sealed record PdfIngestionMessage(
    Guid JobId,
    byte[] PdfBytes) : ContentIngestionMessage(JobId);

/// <summary>
/// Message for YouTube transcript ingestion jobs.
/// </summary>
public sealed record TranscriptIngestionMessage(
    Guid JobId,
    string Transcript) : ContentIngestionMessage(JobId);

/// <summary>
/// Message for documentation scraping jobs.
/// </summary>
public sealed record DocsScrapeMessage(
    Guid JobId) : ContentIngestionMessage(JobId);
