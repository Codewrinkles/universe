using PulseEntity = Codewrinkles.Domain.Pulse.Pulse;
using PulseEngagementEntity = Codewrinkles.Domain.Pulse.PulseEngagement;
using PulseLikeEntity = Codewrinkles.Domain.Pulse.PulseLike;
using PulseImageEntity = Codewrinkles.Domain.Pulse.PulseImage;

namespace Codewrinkles.Application.Common.Interfaces;

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
    /// </summary>
    Task<IReadOnlyList<PulseEntity>> GetFeedAsync(
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
    /// Get all replies for a parent pulse, ordered by CreatedAt ASC.
    /// Includes Author, Engagement, and Image for each reply.
    /// Excludes deleted replies.
    /// </summary>
    Task<IReadOnlyList<PulseEntity>> GetRepliesByParentIdAsync(
        Guid parentPulseId,
        int limit,
        DateTime? beforeCreatedAt,
        Guid? beforeId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get count of replies for a parent pulse (excludes deleted).
    /// </summary>
    Task<int> GetReplyCountAsync(
        Guid parentPulseId,
        CancellationToken cancellationToken);
}
