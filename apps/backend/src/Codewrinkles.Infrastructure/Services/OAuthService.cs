using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Identity;
using Microsoft.Extensions.Configuration;

namespace Codewrinkles.Infrastructure.Services;

public sealed class OAuthService : IOAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    private const string GoogleAuthUrl = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string GoogleTokenUrl = "https://oauth2.googleapis.com/token";
    private const string GoogleUserInfoUrl = "https://www.googleapis.com/oauth2/v2/userinfo";

    private const string GitHubAuthUrl = "https://github.com/login/oauth/authorize";
    private const string GitHubTokenUrl = "https://github.com/login/oauth/access_token";
    private const string GitHubUserInfoUrl = "https://api.github.com/user";

    public OAuthService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<OAuthTokenResponse> ExchangeCodeForTokenAsync(
        OAuthProvider provider,
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        return provider switch
        {
            OAuthProvider.Google => await ExchangeGoogleCodeForTokenAsync(code, redirectUri, cancellationToken),
            OAuthProvider.GitHub => await ExchangeGitHubCodeForTokenAsync(code, redirectUri, cancellationToken),
            _ => throw new ArgumentException($"Unsupported OAuth provider: {provider}")
        };
    }

    public async Task<OAuthUserInfo> GetUserInfoAsync(
        OAuthProvider provider,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        return provider switch
        {
            OAuthProvider.Google => await GetGoogleUserInfoAsync(accessToken, cancellationToken),
            OAuthProvider.GitHub => await GetGitHubUserInfoAsync(accessToken, cancellationToken),
            _ => throw new ArgumentException($"Unsupported OAuth provider: {provider}")
        };
    }

    public OAuthAuthorizationUrl GenerateAuthorizationUrl(
        OAuthProvider provider,
        string redirectUri,
        string state)
    {
        return provider switch
        {
            OAuthProvider.Google => GenerateGoogleAuthorizationUrl(redirectUri, state),
            OAuthProvider.GitHub => GenerateGitHubAuthorizationUrl(redirectUri, state),
            _ => throw new ArgumentException($"Unsupported OAuth provider: {provider}")
        };
    }

    // Google OAuth implementation
    private async Task<OAuthTokenResponse> ExchangeGoogleCodeForTokenAsync(
        string code,
        string redirectUri,
        CancellationToken cancellationToken)
    {
        var clientId = _configuration["OAuth:Google:ClientId"]
            ?? throw new InvalidOperationException("Google OAuth ClientId not configured");
        var clientSecret = _configuration["OAuth:Google:ClientSecret"]
            ?? throw new InvalidOperationException("Google OAuth ClientSecret not configured");

        var requestData = new Dictionary<string, string>
        {
            { "code", code },
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "redirect_uri", redirectUri },
            { "grant_type", "authorization_code" }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, GoogleTokenUrl)
        {
            Content = new FormUrlEncodedContent(requestData)
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenData = JsonSerializer.Deserialize<GoogleTokenResponse>(responseContent)
            ?? throw new InvalidOperationException("Failed to parse Google token response");

        return new OAuthTokenResponse(
            tokenData.access_token,
            tokenData.refresh_token,
            tokenData.expires_in,
            tokenData.token_type,
            tokenData.scope);
    }

    private async Task<OAuthUserInfo> GetGoogleUserInfoAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, GoogleUserInfoUrl);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(responseContent)
            ?? throw new InvalidOperationException("Failed to parse Google user info response");

        return new OAuthUserInfo(
            userInfo.id,
            userInfo.email,
            userInfo.verified_email,
            userInfo.name,
            userInfo.given_name,
            userInfo.family_name,
            userInfo.picture,
            userInfo.locale);
    }

    private OAuthAuthorizationUrl GenerateGoogleAuthorizationUrl(string redirectUri, string state)
    {
        var clientId = _configuration["OAuth:Google:ClientId"]
            ?? throw new InvalidOperationException("Google OAuth ClientId not configured");

        var queryParams = new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "redirect_uri", redirectUri },
            { "response_type", "code" },
            { "scope", "openid email profile" },
            { "state", state },
            { "access_type", "offline" },
            { "prompt", "consent" }
        };

        var queryString = string.Join("&", queryParams.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        return new OAuthAuthorizationUrl($"{GoogleAuthUrl}?{queryString}", state);
    }

    // GitHub OAuth implementation
    private async Task<OAuthTokenResponse> ExchangeGitHubCodeForTokenAsync(
        string code,
        string redirectUri,
        CancellationToken cancellationToken)
    {
        var clientId = _configuration["OAuth:GitHub:ClientId"]
            ?? throw new InvalidOperationException("GitHub OAuth ClientId not configured");
        var clientSecret = _configuration["OAuth:GitHub:ClientSecret"]
            ?? throw new InvalidOperationException("GitHub OAuth ClientSecret not configured");

        var requestData = new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "code", code },
            { "redirect_uri", redirectUri }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, GitHubTokenUrl)
        {
            Content = new FormUrlEncodedContent(requestData)
        };
        request.Headers.Add("Accept", "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenData = JsonSerializer.Deserialize<GitHubTokenResponse>(responseContent)
            ?? throw new InvalidOperationException("Failed to parse GitHub token response");

        return new OAuthTokenResponse(
            tokenData.access_token,
            null,  // GitHub doesn't provide refresh tokens in the basic flow
            3600,  // Default to 1 hour
            tokenData.token_type,
            tokenData.scope);
    }

    private async Task<OAuthUserInfo> GetGitHubUserInfoAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, GitHubUserInfoUrl);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        request.Headers.Add("User-Agent", "Codewrinkles");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var userInfo = JsonSerializer.Deserialize<GitHubUserInfo>(responseContent)
            ?? throw new InvalidOperationException("Failed to parse GitHub user info response");

        // GitHub doesn't always return email in the main user endpoint
        // If email is null, fetch from the emails endpoint
        string? email = userInfo.email;
        if (string.IsNullOrWhiteSpace(email))
        {
            email = await GetGitHubPrimaryEmailAsync(accessToken, cancellationToken);
        }

        return new OAuthUserInfo(
            userInfo.id.ToString(),
            email ?? throw new InvalidOperationException("No email found in GitHub account"),
            true,  // GitHub emails are considered verified
            userInfo.name,
            null,  // GitHub doesn't provide given_name
            null,  // GitHub doesn't provide family_name
            userInfo.avatar_url,
            null);
    }

    private async Task<string?> GetGitHubPrimaryEmailAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        request.Headers.Add("User-Agent", "Codewrinkles");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var emails = JsonSerializer.Deserialize<GitHubEmail[]>(responseContent)
            ?? throw new InvalidOperationException("Failed to parse GitHub emails response");

        // Return the primary verified email
        return emails.FirstOrDefault(e => e.primary && e.verified)?.email;
    }

    private OAuthAuthorizationUrl GenerateGitHubAuthorizationUrl(string redirectUri, string state)
    {
        var clientId = _configuration["OAuth:GitHub:ClientId"]
            ?? throw new InvalidOperationException("GitHub OAuth ClientId not configured");

        var queryParams = new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "redirect_uri", redirectUri },
            { "state", state },
            { "scope", "read:user user:email" }
        };

        var queryString = string.Join("&", queryParams.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        return new OAuthAuthorizationUrl($"{GitHubAuthUrl}?{queryString}", state);
    }

    // Helper: Generate cryptographically secure state parameter
    public static string GenerateState()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes);
    }

    // DTOs for API responses
    private sealed record GoogleTokenResponse(
        string access_token,
        int expires_in,
        string token_type,
        string? refresh_token,
        string? scope);

    private sealed record GoogleUserInfo(
        string id,
        string email,
        bool verified_email,
        string? name,
        string? given_name,
        string? family_name,
        string? picture,
        string? locale);

    private sealed record GitHubTokenResponse(
        string access_token,
        string token_type,
        string scope);

    private sealed record GitHubUserInfo(
        long id,
        string? email,
        string? name,
        string? avatar_url);

    private sealed record GitHubEmail(
        string email,
        bool primary,
        bool verified);
}
