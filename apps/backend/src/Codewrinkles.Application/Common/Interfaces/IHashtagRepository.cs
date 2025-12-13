namespace Codewrinkles.Application.Common.Interfaces;

public interface IHashtagRepository
{
    /// <summary>
    /// Finds a hashtag by its normalized tag (case-insensitive lookup).
    /// </summary>
    Task<Domain.Pulse.Hashtag?> FindByTagAsync(string tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets trending hashtags ordered by pulse count and last used timestamp.
    /// </summary>
    Task<List<Domain.Pulse.Hashtag>> GetTrendingHashtagsAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new hashtag.
    /// </summary>
    void CreateHashtag(Domain.Pulse.Hashtag hashtag);

    /// <summary>
    /// Creates a new pulse-hashtag association.
    /// </summary>
    void CreatePulseHashtag(Domain.Pulse.PulseHashtag pulseHashtag);

    /// <summary>
    /// Gets pulses that contain a specific hashtag, ordered by creation date (cursor-based pagination).
    /// </summary>
    Task<List<Domain.Pulse.Pulse>> GetPulsesByHashtagAsync(
        string tag,
        int limit,
        DateTimeOffset? beforeCreatedAt,
        Guid? beforeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all hashtags associated with a pulse.
    /// Used for decrementing usage counts when editing a pulse.
    /// </summary>
    Task<List<Domain.Pulse.Hashtag>> GetHashtagsForPulseAsync(
        Guid pulseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all pulse-hashtag associations for a pulse.
    /// Does not save to database - call UnitOfWork.SaveChangesAsync().
    /// </summary>
    Task DeletePulseHashtagsForPulseAsync(
        Guid pulseId,
        CancellationToken cancellationToken = default);
}
