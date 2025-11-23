using Codewrinkles.Domain.Identity;

namespace Codewrinkles.Application.Common.Interfaces;

public interface IProfileRepository
{
    /// <summary>
    /// Check if a profile with the given handle exists (case-insensitive).
    /// Encapsulates handle normalization logic.
    /// </summary>
    Task<bool> ExistsByHandleAsync(string handle, CancellationToken cancellationToken);

    /// <summary>
    /// Find profile by handle (case-insensitive).
    /// Returns null if not found.
    /// </summary>
    Task<Profile?> FindByHandleAsync(string handle, CancellationToken cancellationToken);

    /// <summary>
    /// Find profile by identity ID.
    /// Returns null if not found.
    /// </summary>
    Task<Profile?> FindByIdentityIdAsync(Guid identityId, CancellationToken cancellationToken);

    /// <summary>
    /// Find profile by ID.
    /// Returns null if not found.
    /// </summary>
    Task<Profile?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Create a new profile in the system.
    /// Does not save to database - call UnitOfWork.SaveChangesAsync().
    /// </summary>
    void Create(Profile profile);
}
