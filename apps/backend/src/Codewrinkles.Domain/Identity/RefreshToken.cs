namespace Codewrinkles.Domain.Identity;

/// <summary>
/// Represents a refresh token used for obtaining new access tokens
/// Supports token rotation for enhanced security
/// </summary>
public sealed class RefreshToken
{
    // Primary key
    public Guid Id { get; private set; }

    // Token value (hashed for security)
    public string TokenHash { get; private set; }

    // Owning identity
    public Guid IdentityId { get; private set; }
    public Identity Identity { get; private set; }

    // Expiration and validity
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }

    // Token rotation tracking
    public Guid? ReplacedByTokenId { get; private set; }
    public RefreshToken? ReplacedByToken { get; private set; }

    // Private parameterless constructor for EF Core materialization only
    // EF Core will populate all properties via reflection when loading from database
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
    private RefreshToken() { }
#pragma warning restore CS8618

    // Factory method for creating new refresh tokens
    public static RefreshToken Create(
        string tokenHash,
        Guid identityId,
        DateTime expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        if (expiresAt <= DateTime.UtcNow)
        {
            throw new ArgumentException("Expiration date must be in the future", nameof(expiresAt));
        }

        return new RefreshToken
        {
            Id = Guid.NewGuid(), // Will be overwritten by EF Core sequential GUID
            TokenHash = tokenHash,
            IdentityId = identityId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            IsUsed = false,
            IsRevoked = false
        };
    }

    /// <summary>
    /// Checks if the refresh token is valid (not expired, used, or revoked)
    /// </summary>
    public bool IsValid()
    {
        return !IsExpired() && !IsUsed && !IsRevoked;
    }

    /// <summary>
    /// Checks if the token has expired
    /// </summary>
    public bool IsExpired()
    {
        return DateTime.UtcNow >= ExpiresAt;
    }

    /// <summary>
    /// Marks the token as used and records the replacement token (for rotation)
    /// </summary>
    public void MarkAsUsed(Guid replacementTokenId)
    {
        IsUsed = true;
        ReplacedByTokenId = replacementTokenId;
    }

    /// <summary>
    /// Revokes the token with a reason
    /// </summary>
    public void Revoke(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevokedReason = reason;
    }
}
