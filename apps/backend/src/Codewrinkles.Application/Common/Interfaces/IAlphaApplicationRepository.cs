using Codewrinkles.Domain.Nova;

namespace Codewrinkles.Application.Common.Interfaces;

public interface IAlphaApplicationRepository
{
    /// <summary>
    /// Create a new alpha application.
    /// Does not save to database - call UnitOfWork.SaveChangesAsync().
    /// </summary>
    void Create(AlphaApplication application);

    /// <summary>
    /// Find an application by ID.
    /// </summary>
    Task<AlphaApplication?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Find an application by invite code.
    /// </summary>
    Task<AlphaApplication?> FindByInviteCodeAsync(string inviteCode, CancellationToken cancellationToken);

    /// <summary>
    /// Find an application by email.
    /// </summary>
    Task<AlphaApplication?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Check if an application exists for the given email.
    /// </summary>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Get all applications, ordered by creation date (newest first).
    /// </summary>
    Task<IReadOnlyList<AlphaApplication>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get applications by status, ordered by creation date (newest first).
    /// </summary>
    Task<IReadOnlyList<AlphaApplication>> GetByStatusAsync(
        AlphaApplicationStatus status,
        CancellationToken cancellationToken);
}
