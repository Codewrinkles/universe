using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Identity;
using Codewrinkles.Telemetry;
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
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Authentication.TokenRefresh);

        try
        {
            // 1. Hash the provided refresh token to look it up in database
            var tokenHash = JwtTokenGenerator.HashToken(request.RefreshToken);

            // 2. Find the refresh token in database by hash
            var refreshToken = await _unitOfWork.RefreshTokens.FindByTokenHashAsync(tokenHash, cancellationToken);

            if (refreshToken == null)
            {
                AppMetrics.RecordTokenRefresh(success: false);
                activity?.SetTag(TagNames.Auth.FailureReason, "token_not_found");
                throw new InvalidRefreshTokenException("Invalid refresh token");
            }

            activity?.SetIdentityId(refreshToken.IdentityId);

            // 3. Validate the refresh token
            if (!refreshToken.IsValid())
            {
                AppMetrics.RecordTokenRefresh(success: false);

                if (refreshToken.IsExpired())
                {
                    activity?.SetTag(TagNames.Auth.TokenExpired, true);
                    activity?.SetTag(TagNames.Auth.FailureReason, "expired");
                    throw new RefreshTokenExpiredException("Refresh token has expired");
                }

                if (refreshToken.IsUsed)
                {
                    activity?.SetTag(TagNames.Auth.FailureReason, "already_used");
                    throw new InvalidRefreshTokenException("Refresh token has already been used");
                }

                if (refreshToken.IsRevoked)
                {
                    activity?.SetTag(TagNames.Auth.FailureReason, "revoked");
                    throw new InvalidRefreshTokenException("Refresh token has been revoked");
                }

                activity?.SetTag(TagNames.Auth.FailureReason, "invalid");
                throw new InvalidRefreshTokenException("Refresh token is invalid");
            }

            // 4. Load the associated identity and profile
            var identity = await _unitOfWork.Identities.FindByIdAsync(refreshToken.IdentityId, cancellationToken);

            if (identity == null)
            {
                AppMetrics.RecordTokenRefresh(success: false);
                activity?.SetTag(TagNames.Auth.FailureReason, "user_not_found");
                throw new InvalidRefreshTokenException("Associated user not found");
            }

            // Ensure profile is loaded
            var profile = identity.Profile;
            activity?.SetUserContext(identity.Id, profile.Id);

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

            // Record metrics
            AppMetrics.RecordTokenRefresh(success: true);
            activity?.SetSuccess(true);

            // 8. Return new tokens
            return new RefreshAccessTokenResult(
                AccessToken: newAccessToken,
                RefreshToken: newRefreshToken
            );
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
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
