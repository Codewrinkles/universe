using Codewrinkles.Domain.Identity;

namespace Codewrinkles.Application.Common.Interfaces;

public interface IOAuthService
{
    Task<OAuthTokenResponse> ExchangeCodeForTokenAsync(
        OAuthProvider provider,
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default);

    Task<OAuthUserInfo> GetUserInfoAsync(
        OAuthProvider provider,
        string accessToken,
        CancellationToken cancellationToken = default);

    OAuthAuthorizationUrl GenerateAuthorizationUrl(
        OAuthProvider provider,
        string redirectUri,
        string state);
}

public sealed record OAuthTokenResponse(
    string AccessToken,
    string? RefreshToken,
    int ExpiresIn,
    string TokenType,
    string? Scope);

public sealed record OAuthUserInfo(
    string ProviderUserId,
    string Email,
    bool EmailVerified,
    string? Name,
    string? GivenName,
    string? FamilyName,
    string? Picture,
    string? Locale);

public sealed record OAuthAuthorizationUrl(
    string Url,
    string State);
