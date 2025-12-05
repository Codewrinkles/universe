using PulseEntity = Codewrinkles.Domain.Pulse.Pulse;
using PulseEngagementEntity = Codewrinkles.Domain.Pulse.PulseEngagement;
using PulseLikeEntity = Codewrinkles.Domain.Pulse.PulseLike;
using PulseImageEntity = Codewrinkles.Domain.Pulse.PulseImage;
using PulseMentionEntity = Codewrinkles.Domain.Pulse.PulseMention;
using PulseLinkPreviewEntity = Codewrinkles.Domain.Pulse.PulseLinkPreview;

namespace Codewrinkles.Application.Common.Interfaces;

/// <summary>
/// Aggregated feed data containing pulses and all associated metadata.
/// Used by GetFeedWithMetadataAsync to return everything in a single call.
/// </summary>
public sealed record FeedData(
    IReadOnlyList<PulseEntity> Pulses,
    HashSet<Guid> LikedPulseIds,
    HashSet<Guid> BookmarkedPulseIds,
    HashSet<Guid> FollowingProfileIds,
    IReadOnlyList<PulseMentionEntity> Mentions
);

public interface IPulseRepository
{
    /// <summary>
    /// Find pulse by ID.
    /// Returns null if not found.
    /// </summary>
    Task<PulseEntity?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Find pulse by ID with Author, Engagement, and RepulsedPulse eagerly loaded.
    /// Returns null if not found.
    /// </summary>
    Task<PulseEntity?> FindByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Get paginated feed of pulses (not deleted), ordered by CreatedAt DESC.
    /// Includes Author and Engagement.
    /// If currentUserId is provided, filters to show only pulses from followed users + own pulses.
    /// If currentUserId is null, returns all pulses (public feed).
    /// </summary>
    Task<IReadOnlyList<PulseEntity>> GetFeedAsync(
        Guid? currentUserId,
        int limit,
        DateTime? beforeCreatedAt,
        Guid? beforeId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get paginated feed with all associated metadata in a single repository call.
    /// Uses parallel queries internally for optimal performance.
    /// Returns pulses along with liked/bookmarked/following/mentions metadata.
    /// If currentUserId is null, metadata sets will be empty.
    /// </summary>
    Task<FeedData> GetFeedWithMetadataAsync(
        Guid? currentUserId,
        int limit,
        DateTime? beforeCreatedAt,
        Guid? beforeId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get pulses by author ID, ordered by CreatedAt DESC.
    /// Includes Engagement.
    /// </summary>
    Task<IReadOnlyList<PulseEntity>> GetByAuthorIdAsync(
        Guid authorId,
        int limit,
        DateTime? beforeCreatedAt,
        Guid? beforeId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get pulses by author with all associated metadata in a single repository call.
    /// Uses parallel queries internally for optimal performance.
    /// Returns pulses along with liked/bookmarked/following/mentions metadata.
    /// If currentUserId is null, metadata sets will be empty.
    /// </summary>
    Task<FeedData> GetPulsesByAuthorWithMetadataAsync(
        Guid authorId,
        Guid? currentUserId,
        int limit,
        DateTime? beforeCreatedAt,
        Guid? beforeId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Create a new pulse.
    /// Does not save to database - call UnitOfWork.SaveChangesAsync().
    /// </summary>
    void Create(PulseEntity pulse);

    /// <summary>
    /// Create pulse engagement for a pulse.
    /// Does not save to database - call UnitOfWork.SaveChangesAsync().
    /// </summary>
    void CreateEngagement(PulseEngagementEntity engagement);

    /// <summary>
    /// Find pulse engagement by pulse ID (with tracking for updates).
    /// Returns null if not found.
    /// </summary>
    Task<PulseEngagementEntity?> FindEngagementAsync(Guid pulseId, CancellationToken cancellationToken);

    /// <summary>
    /// Delete a pulse (soft delete will be handled by domain entity).
    /// Does not save to database - call UnitOfWork.SaveChangesAsync().
    /// </summary>
    void Delete(PulseEntity pulse);

    /// <summary>
    /// Find a like by pulse ID and profile ID.
    /// Returns null if not found.
    /// </summary>
    Task<PulseLikeEntity?> FindLikeAsync(
        Guid pulseId,
        Guid profileId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Check if a user has liked a pulse.
    /// </summary>
    Task<bool> HasUserLikedPulseAsync(
        Guid pulseId,
        Guid profileId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get liked pulse IDs for a user from a set of pulse IDs.
    /// Returns a set of pulse IDs that the user has liked.
    /// </summary>
    Task<HashSet<Guid>> GetLikedPulseIdsAsync(
        IEnumerable<Guid> pulseIds,
        Guid profileId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Create a new like.
    /// Does not save to database - call UnitOfWork.SaveChangesAsync().
    /// </summary>
    void CreateLike(PulseLikeEntity like);

    /// <summary>
    /// Delete a like.
    /// Does not save to database - call UnitOfWork.SaveChangesAsync().
    /// </summary>
    void DeleteLike(PulseLikeEntity like);

    /// <summary>
    /// Create a pulse image.
    /// Does not save to database - call UnitOfWork.SaveChangesAsync().
    /// </summary>
    void CreateImage(PulseImageEntity image);

    /// <summary>
    /// Create a pulse link preview.
    /// Does not save to database - call UnitOfWork.SaveChangesAsync().
    /// </summary>
    void CreateLinkPreview(PulseLinkPreviewEntity linkPreview);

    /// <summary>
    /// Get all replies in a thread, ordered by CreatedAt ASC.
    /// Includes Author, Engagement, Image, and ParentPulse (for "Replying to" context).
    /// Excludes deleted replies.
    /// </summary>
    Task<IReadOnlyList<PulseEntity>> GetRepliesByThreadRootIdAsync(
        Guid threadRootId,
        int limit,
        DateTime? beforeCreatedAt,
        Guid? beforeId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get count of replies in a thread (excludes deleted).
    /// </summary>
    Task<int> GetReplyCountByThreadRootIdAsync(
        Guid threadRootId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get count of pulses by author (excludes deleted).
    /// </summary>
    Task<int> GetPulseCountByAuthorAsync(
        Guid authorId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Create a pulse mention.
    /// Does not save to database - call UnitOfWork.SaveChangesAsync().
    /// </summary>
    void CreateMention(PulseMentionEntity mention);

    /// <summary>
    /// Get mentions for a list of pulse IDs.
    /// Returns all mentions for the given pulses.
    /// </summary>
    Task<IReadOnlyList<PulseMentionEntity>> GetMentionsForPulsesAsync(
        IEnumerable<Guid> pulseIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total count of all pulses (excludes deleted).
    /// </summary>
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken);
}
