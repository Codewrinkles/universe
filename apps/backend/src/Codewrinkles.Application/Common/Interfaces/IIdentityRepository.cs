using Codewrinkles.Domain.Identity;

namespace Codewrinkles.Application.Common.Interfaces;

public interface IIdentityRepository
{
    /// <summary>
    /// Check if an identity with the given email exists (case-insensitive).
    /// Encapsulates email normalization logic.
    /// </summary>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Find identity by email (case-insensitive).
    /// Returns null if not found.
    /// </summary>
    Task<Identity?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Find identity by email with Profile eagerly loaded (case-insensitive).
    /// Returns null if not found.
    /// </summary>
    Task<Identity?> FindByEmailWithProfileAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Find identity by ID.
    /// Returns null if not found.
    /// </summary>
    Task<Identity?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Register a new identity in the system.
    /// Does not save to database - call UnitOfWork.SaveChangesAsync().
    /// </summary>
    void Register(Identity identity);
}
