using System.Threading.Channels;

namespace Codewrinkles.Infrastructure.Email;

/// <summary>
/// In-memory channel for queuing emails to be sent by the background service.
/// </summary>
public sealed class EmailChannel
{
    private readonly Channel<QueuedEmail> _channel;

    public EmailChannel()
    {
        // Unbounded: emails queue up, never block the producer
        // SingleReader: only one background service reads
        _channel = Channel.CreateUnbounded<QueuedEmail>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ValueTask WriteAsync(QueuedEmail message, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(message, ct);

    public IAsyncEnumerable<QueuedEmail> ReadAllAsync(CancellationToken ct = default)
        => _channel.Reader.ReadAllAsync(ct);
}
