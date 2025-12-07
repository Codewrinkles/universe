namespace Codewrinkles.Domain.Identity;

public sealed class ExternalLogin
{
    public Guid Id { get; private set; }
    public Guid IdentityId { get; private set; }
    public OAuthProvider Provider { get; private set; }
    public string ProviderUserId { get; private set; }
    public string ProviderEmail { get; private set; }
    public string? ProviderDisplayName { get; private set; }
    public string? ProviderAvatarUrl { get; private set; }
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTimeOffset? TokenExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Identity Identity { get; private set; }

    // Private parameterless constructor for EF Core materialization only
    // EF Core will populate all properties via reflection when loading from database
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
    private ExternalLogin() { }
#pragma warning restore CS8618

    public static ExternalLogin Create(
        Guid identityId,
        OAuthProvider provider,
        string providerUserId,
        string providerEmail,
        string? providerDisplayName = null,
        string? providerAvatarUrl = null,
        string? accessToken = null,
        string? refreshToken = null,
        DateTimeOffset? tokenExpiresAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerEmail);

        return new ExternalLogin
        {
            IdentityId = identityId,
            Provider = provider,
            ProviderUserId = providerUserId,
            ProviderEmail = providerEmail.Trim().ToLowerInvariant(),
            ProviderDisplayName = providerDisplayName?.Trim(),
            ProviderAvatarUrl = providerAvatarUrl?.Trim(),
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenExpiresAt = tokenExpiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateTokens(string accessToken, string? refreshToken, DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        AccessToken = accessToken;
        if (!string.IsNullOrWhiteSpace(refreshToken)) RefreshToken = refreshToken;
        TokenExpiresAt = expiresAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
