using Codewrinkles.Domain.Pulse;

namespace Codewrinkles.Application.Common.Interfaces;

public interface IBookmarkRepository
{
    /// <summary>
    /// Creates a new bookmark
    /// </summary>
    void Create(PulseBookmark bookmark);

    /// <summary>
    /// Deletes a bookmark
    /// </summary>
    void Delete(PulseBookmark bookmark);

    /// <summary>
    /// Finds a bookmark by profile and pulse IDs
    /// </summary>
    Task<PulseBookmark?> FindByProfileAndPulseAsync(
        Guid profileId,
        Guid pulseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has bookmarked a specific pulse
    /// </summary>
    Task<bool> IsBookmarkedAsync(
        Guid profileId,
        Guid pulseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets bookmarked pulse IDs for multiple pulses (for isBookmarked flag in feed)
    /// </summary>
    Task<HashSet<Guid>> GetBookmarkedPulseIdsAsync(
        IEnumerable<Guid> pulseIds,
        Guid profileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated bookmarked pulses for a user, ordered by bookmark date DESC
    /// Includes full pulse data with author, engagement, etc.
    /// </summary>
    Task<(List<Domain.Pulse.Pulse> Pulses, bool HasMore)> GetBookmarkedPulsesAsync(
        Guid profileId,
        int limit,
        DateTimeOffset? beforeCreatedAt = null,
        Guid? beforeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets bookmarked pulses with all associated metadata in a single repository call.
    /// Uses parallel queries internally for optimal performance.
    /// Returns pulses along with liked/following/mentions metadata.
    /// Bookmarked status is implicit (all returned pulses are bookmarked by definition).
    /// </summary>
    Task<FeedData> GetBookmarkedPulsesWithMetadataAsync(
        Guid profileId,
        int limit,
        DateTimeOffset? beforeCreatedAt = null,
        Guid? beforeId = null,
        CancellationToken cancellationToken = default);
}
