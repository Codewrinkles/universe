using System.Threading.Channels;

namespace Codewrinkles.Infrastructure.Services.Nova;

/// <summary>
/// Singleton channel for memory extraction jobs.
/// Fire-and-forget pattern: producers write to channel, background service consumes.
/// </summary>
public sealed class MemoryExtractionChannel
{
    private readonly Channel<MemoryExtractionMessage> _channel;

    public MemoryExtractionChannel()
    {
        // Unbounded channel - jobs queue up without blocking
        // For production, consider BoundedChannelOptions with backpressure
        _channel = Channel.CreateUnbounded<MemoryExtractionMessage>(
            new UnboundedChannelOptions
            {
                SingleReader = true,  // Only one background service reads
                SingleWriter = false  // Multiple endpoints can queue
            });
    }

    public ValueTask WriteAsync(MemoryExtractionMessage message, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(message, ct);

    public IAsyncEnumerable<MemoryExtractionMessage> ReadAllAsync(CancellationToken ct = default)
        => _channel.Reader.ReadAllAsync(ct);
}

/// <summary>
/// Message for memory extraction jobs.
/// Contains the profile ID to extract memories for.
/// </summary>
public sealed record MemoryExtractionMessage(Guid ProfileId);
