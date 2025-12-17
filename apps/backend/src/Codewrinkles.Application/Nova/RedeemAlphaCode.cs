using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Identity;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

public sealed record RedeemAlphaCodeCommand(
    Guid ProfileId,
    string Code
) : ICommand<RedeemAlphaCodeResult>;

public sealed record RedeemAlphaCodeResult(
    bool HasNovaAccess,
    string AccessToken,
    string RefreshToken
);

public sealed class RedeemAlphaCodeCommandHandler
    : ICommandHandler<RedeemAlphaCodeCommand, RedeemAlphaCodeResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtTokenGenerator _jwtTokenGenerator;

    public RedeemAlphaCodeCommandHandler(
        IUnitOfWork unitOfWork,
        JwtTokenGenerator jwtTokenGenerator)
    {
        _unitOfWork = unitOfWork;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<RedeemAlphaCodeResult> HandleAsync(
        RedeemAlphaCodeCommand command,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Nova.RedeemAlphaCode);
        activity?.SetProfileId(command.ProfileId);

        try
        {
            // Validator has already confirmed:
            // - Profile exists
            // - Profile doesn't already have Nova access
            // - Code is valid and not redeemed

            // Find the application by invite code
            var application = await _unitOfWork.AlphaApplications
                .FindByInviteCodeAsync(command.Code.Trim().ToUpperInvariant(), cancellationToken);

            // Validator ensures this exists, but null check for safety
            if (application is null)
            {
                throw new InvalidAlphaCodeException();
            }

            // Mark code as redeemed
            application.MarkCodeRedeemed(command.ProfileId);

            // Grant Alpha access to the profile
            var profile = await _unitOfWork.Profiles.FindByIdAsync(command.ProfileId, cancellationToken);
            if (profile is null)
            {
                throw new ProfileNotFoundException(command.ProfileId);
            }

            profile.GrantAlphaAccess();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Generate new tokens with updated hasNovaAccess claim
            var identity = await _unitOfWork.Identities.FindByIdAsync(profile.IdentityId, cancellationToken);
            if (identity is null)
            {
                throw new ProfileNotFoundException(command.ProfileId);
            }

            var accessToken = _jwtTokenGenerator.GenerateAccessToken(identity, profile);

            // Generate refresh token and store in database
            var (refreshToken, refreshTokenHash) = JwtTokenGenerator.GenerateRefreshToken();
            var refreshTokenExpiry = DateTimeOffset.UtcNow.AddDays(_jwtTokenGenerator.RefreshTokenExpiryDays);

            var refreshTokenEntity = RefreshToken.Create(
                refreshTokenHash,
                identity.Id,
                refreshTokenExpiry
            );

            _unitOfWork.RefreshTokens.Add(refreshTokenEntity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            activity?.SetSuccess(true);
            activity?.SetTag("code_redeemed", "true");

            return new RedeemAlphaCodeResult(
                HasNovaAccess: true,
                AccessToken: accessToken,
                RefreshToken: refreshToken
            );
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}

/// <summary>
/// Thrown when an invalid or already-used invite code is provided.
/// </summary>
public sealed class InvalidAlphaCodeException : Exception
{
    public InvalidAlphaCodeException()
        : base("Invalid or already used invite code") { }
}

/// <summary>
/// Thrown when a profile is not found.
/// </summary>
public sealed class ProfileNotFoundException : Exception
{
    public Guid ProfileId { get; }

    public ProfileNotFoundException(Guid profileId)
        : base($"Profile with ID {profileId} was not found")
    {
        ProfileId = profileId;
    }
}

/// <summary>
/// Thrown when a user already has Nova access.
/// </summary>
public sealed class AlreadyHasNovaAccessException : Exception
{
    public AlreadyHasNovaAccessException()
        : base("You already have Nova access") { }
}
