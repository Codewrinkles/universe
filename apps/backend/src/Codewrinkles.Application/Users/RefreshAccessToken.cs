using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Identity;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Users;

// Command
public sealed record RefreshAccessTokenCommand(string RefreshToken) : ICommand<RefreshAccessTokenResult>;

// Result
public sealed record RefreshAccessTokenResult(
    string AccessToken,
    string RefreshToken
);

// Handler
public sealed class RefreshAccessTokenCommandHandler : ICommandHandler<RefreshAccessTokenCommand, RefreshAccessTokenResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtTokenGenerator _jwtTokenGenerator;

    public RefreshAccessTokenCommandHandler(
        IUnitOfWork unitOfWork,
        JwtTokenGenerator jwtTokenGenerator)
    {
        _unitOfWork = unitOfWork;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<RefreshAccessTokenResult> HandleAsync(RefreshAccessTokenCommand request, CancellationToken cancellationToken)
    {
        // 1. Hash the provided refresh token to look it up in database
        var tokenHash = JwtTokenGenerator.HashToken(request.RefreshToken);

        // 2. Find the refresh token in database by hash
        var refreshToken = await _unitOfWork.RefreshTokens.FindByTokenHashAsync(tokenHash, cancellationToken);

        if (refreshToken == null)
        {
            throw new InvalidRefreshTokenException("Invalid refresh token");
        }

        // 3. Validate the refresh token
        if (!refreshToken.IsValid())
        {
            if (refreshToken.IsExpired())
            {
                throw new RefreshTokenExpiredException("Refresh token has expired");
            }

            if (refreshToken.IsUsed)
            {
                throw new InvalidRefreshTokenException("Refresh token has already been used");
            }

            if (refreshToken.IsRevoked)
            {
                throw new InvalidRefreshTokenException("Refresh token has been revoked");
            }

            throw new InvalidRefreshTokenException("Refresh token is invalid");
        }

        // 4. Load the associated identity and profile
        var identity = await _unitOfWork.Identities.FindByIdAsync(refreshToken.IdentityId, cancellationToken);

        if (identity == null)
        {
            throw new InvalidRefreshTokenException("Associated user not found");
        }

        // Ensure profile is loaded
        var profile = identity.Profile;

        // 5. Generate new access token
        var newAccessToken = _jwtTokenGenerator.GenerateAccessToken(identity, profile);

        // 6. Generate new refresh token (token rotation)
        var (newRefreshToken, newTokenHash) = JwtTokenGenerator.GenerateRefreshToken();
        var newRefreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtTokenGenerator.RefreshTokenExpiryDays);

        var newRefreshTokenEntity = RefreshToken.Create(
            newTokenHash,
            identity.Id,
            newRefreshTokenExpiry
        );

        // 7. Mark old refresh token as used and link to new one
        _unitOfWork.RefreshTokens.Add(newRefreshTokenEntity);
        await _unitOfWork.SaveChangesAsync(cancellationToken); // Save to get ID

        refreshToken.MarkAsUsed(newRefreshTokenEntity.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 8. Return new tokens
        return new RefreshAccessTokenResult(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken
        );
    }
}

// Custom exceptions for better error handling
public sealed class InvalidRefreshTokenException : ApplicationException
{
    public InvalidRefreshTokenException(string message) : base(message) { }
}

public sealed class RefreshTokenExpiredException : ApplicationException
{
    public RefreshTokenExpiredException(string message) : base(message) { }
}
