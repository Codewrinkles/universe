using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Codewrinkles.Domain.Identity;

public sealed class JwtTokenGenerator
{
    private readonly JwtSettings _settings;

    public JwtTokenGenerator(JwtSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Gets the number of days until a refresh token expires
    /// </summary>
    public int RefreshTokenExpiryDays => _settings.RefreshTokenExpiryDays;

    public string GenerateAccessToken(Identity identity, Profile profile)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, identity.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, identity.Email),
            new(JwtRegisteredClaimNames.Name, profile.Name),
            new("profileId", profile.Id.ToString()),
            new("role", identity.Role.ToString()),
            new("hasNovaAccess", profile.HasNovaAccess.ToString().ToLowerInvariant()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        // Add handle claim only if it's not null
        if (!string.IsNullOrWhiteSpace(profile.Handle))
        {
            claims.Add(new Claim("handle", profile.Handle));
        }

        // Add avatarUrl claim only if it's not null
        if (!string.IsNullOrWhiteSpace(profile.AvatarUrl))
        {
            claims.Add(new Claim("avatarUrl", profile.AvatarUrl));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTimeOffset.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes).UtcDateTime,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a cryptographically secure refresh token
    /// Returns both the raw token (to send to client) and the hash (to store in database)
    /// </summary>
    public static (string Token, string TokenHash) GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        var token = Convert.ToBase64String(randomBytes);
        var tokenHash = HashToken(token);

        return (token, tokenHash);
    }

    /// <summary>
    /// Hashes a refresh token using SHA256 for secure storage
    /// </summary>
    public static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }
}
