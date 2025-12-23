using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Infrastructure.Services.Nova;

/// <summary>
/// Producer implementation that writes to the memory extraction channel.
/// </summary>
public sealed class MemoryExtractionQueue : IMemoryExtractionQueue
{
    private readonly MemoryExtractionChannel _channel;

    public MemoryExtractionQueue(MemoryExtractionChannel channel)
    {
        _channel = channel;
    }

    public async Task QueueExtractionAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var message = new MemoryExtractionMessage(profileId);
        await _channel.WriteAsync(message, cancellationToken);
    }
}
