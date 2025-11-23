namespace Codewrinkles.Domain.Identity;

public sealed record JwtSettings(
    string SecretKey,
    string Issuer,
    string Audience,
    int AccessTokenExpiryMinutes
);
