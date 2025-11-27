using PulseEntity = Codewrinkles.Domain.Pulse.Pulse;
using PulseEngagementEntity = Codewrinkles.Domain.Pulse.PulseEngagement;

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
    /// Delete a pulse (soft delete will be handled by domain entity).
    /// Does not save to database - call UnitOfWork.SaveChangesAsync().
    /// </summary>
    void Delete(PulseEntity pulse);
}
