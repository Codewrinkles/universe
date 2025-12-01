using Codewrinkles.Domain.Identity;

namespace Codewrinkles.Application.Common.Interfaces;

public interface IRefreshTokenRepository
{
    /// <summary>
    /// Find a refresh token by its hash
    /// </summary>
    Task<RefreshToken?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new refresh token
    /// </summary>
    void Add(RefreshToken refreshToken);

    /// <summary>
    /// Revoke all refresh tokens for a specific identity (for logout from all devices)
    /// </summary>
    Task RevokeAllForIdentityAsync(Guid identityId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete expired refresh tokens (cleanup job)
    /// </summary>
    Task DeleteExpiredTokensAsync(CancellationToken cancellationToken = default);
}
