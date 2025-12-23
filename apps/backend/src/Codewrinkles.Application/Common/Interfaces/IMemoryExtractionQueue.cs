namespace Codewrinkles.Application.Common.Interfaces;

/// <summary>
/// Producer interface for queuing memory extraction jobs.
/// Memory extraction analyzes conversation sessions and creates memories for future context.
/// </summary>
public interface IMemoryExtractionQueue
{
    /// <summary>
    /// Queue memory extraction for a profile.
    /// The background service will process all sessions needing extraction for this profile.
    /// </summary>
    /// <param name="profileId">The profile ID to extract memories for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task QueueExtractionAsync(Guid profileId, CancellationToken cancellationToken = default);
}
