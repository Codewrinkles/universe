using System.Data;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Identity;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Users;

public sealed record CompleteOAuthCallbackCommand(
    OAuthProvider Provider,
    string Code,
    string State,
    string RedirectUri
) : ICommand<CompleteOAuthCallbackResult>;

public sealed record CompleteOAuthCallbackResult(
    Guid IdentityId,
    Guid ProfileId,
    string Email,
    string Name,
    string? Handle,
    string? Bio,
    string? AvatarUrl,
    UserRole Role,
    string AccessToken,
    string RefreshToken,
    bool IsNewUser
);

public sealed class CompleteOAuthCallbackCommandHandler
    : ICommandHandler<CompleteOAuthCallbackCommand, CompleteOAuthCallbackResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOAuthService _oAuthService;
    private readonly JwtTokenGenerator _jwtTokenGenerator;

    public CompleteOAuthCallbackCommandHandler(
        IUnitOfWork unitOfWork,
        IOAuthService oAuthService,
        JwtTokenGenerator jwtTokenGenerator)
    {
        _unitOfWork = unitOfWork;
        _oAuthService = oAuthService;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<CompleteOAuthCallbackResult> HandleAsync(
        CompleteOAuthCallbackCommand command,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Authentication.OAuthCallback);
        activity?.SetOAuthProvider(command.Provider.ToString().ToLowerInvariant());

        try
        {
            // Exchange code for tokens
            var tokenResponse = await _oAuthService.ExchangeCodeForTokenAsync(
                command.Provider,
                command.Code,
                command.RedirectUri,
                cancellationToken);

            // Fetch user info
            var userInfo = await _oAuthService.GetUserInfoAsync(
                command.Provider,
                tokenResponse.AccessToken,
                cancellationToken);

            activity?.SetEmailDomain(userInfo.Email);

            // Check existing external login
            var existingExternalLogin = await _unitOfWork.ExternalLogins
                .FindByProviderAndUserIdAsync(
                    command.Provider,
                    userInfo.ProviderUserId,
                    cancellationToken);

            CompleteOAuthCallbackResult result;
            if (existingExternalLogin is not null)
            {
                activity?.SetTag("oauth.flow", "returning_user");
                result = await HandleReturningUserAsync(existingExternalLogin, tokenResponse, cancellationToken);
            }
            else
            {
                // Check existing identity by email
                var identityByEmail = await _unitOfWork.Identities.FindByEmailAsync(
                    userInfo.Email,
                    cancellationToken);

                if (identityByEmail is not null)
                {
                    activity?.SetTag("oauth.flow", "account_linking");
                    result = await HandleAccountLinkingAsync(
                        identityByEmail,
                        command.Provider,
                        userInfo,
                        tokenResponse,
                        cancellationToken);
                }
                else
                {
                    // New user registration
                    activity?.SetTag("oauth.flow", "new_registration");
                    result = await HandleNewUserRegistrationAsync(
                        command.Provider,
                        userInfo,
                        tokenResponse,
                        cancellationToken);
                }
            }

            activity?.SetUserContext(result.IdentityId, result.ProfileId);
            AppMetrics.RecordOAuthCallback(command.Provider.ToString().ToLowerInvariant(), success: true);
            if (result.IsNewUser)
            {
                AppMetrics.RecordUserRegistered($"oauth_{command.Provider.ToString().ToLowerInvariant()}");
            }
            else
            {
                AppMetrics.RecordUserLoggedIn($"oauth_{command.Provider.ToString().ToLowerInvariant()}");
            }
            activity?.SetSuccess(true);

            return result;
        }
        catch (Exception ex)
        {
            AppMetrics.RecordOAuthCallback(command.Provider.ToString().ToLowerInvariant(), success: false);
            activity?.RecordError(ex);
            throw;
        }
    }

    private async Task<CompleteOAuthCallbackResult> HandleReturningUserAsync(
        ExternalLogin externalLogin,
        OAuthTokenResponse tokenResponse,
        CancellationToken cancellationToken)
    {
        var identity = (await _unitOfWork.Identities.FindByIdAsync(
            externalLogin.IdentityId,
            cancellationToken))!;

        var profile = (await _unitOfWork.Profiles.FindByIdentityIdAsync(
            externalLogin.IdentityId,
            cancellationToken))!;

        externalLogin.UpdateTokens(
            tokenResponse.AccessToken,
            tokenResponse.RefreshToken,
            DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn));

        identity.RecordSuccessfulLogin();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(identity, profile);

        // Generate refresh token and store in database
        var (refreshToken, refreshTokenHash) = JwtTokenGenerator.GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtTokenGenerator.RefreshTokenExpiryDays);

        var refreshTokenEntity = RefreshToken.Create(
            refreshTokenHash,
            identity.Id,
            refreshTokenExpiry
        );

        _unitOfWork.RefreshTokens.Add(refreshTokenEntity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CompleteOAuthCallbackResult(
            identity.Id,
            profile.Id,
            identity.Email,
            profile.Name,
            profile.Handle,
            profile.Bio,
            profile.AvatarUrl,
            identity.Role,
            accessToken,
            refreshToken,
            IsNewUser: false);
    }

    private async Task<CompleteOAuthCallbackResult> HandleAccountLinkingAsync(
        Identity existingIdentity,
        OAuthProvider provider,
        OAuthUserInfo userInfo,
        OAuthTokenResponse tokenResponse,
        CancellationToken cancellationToken)
    {
        var externalLogin = ExternalLogin.Create(
            existingIdentity.Id,
            provider,
            userInfo.ProviderUserId,
            userInfo.Email,
            userInfo.Name,
            userInfo.Picture,
            tokenResponse.AccessToken,
            tokenResponse.RefreshToken,
            DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn));

        _unitOfWork.ExternalLogins.Add(externalLogin);

        if (userInfo.EmailVerified)
        {
            existingIdentity.MarkEmailAsVerified();
        }

        existingIdentity.RecordSuccessfulLogin();

        var profile = (await _unitOfWork.Profiles.FindByIdentityIdAsync(
            existingIdentity.Id,
            cancellationToken))!;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(existingIdentity, profile);

        // Generate refresh token and store in database
        var (refreshToken, refreshTokenHash) = JwtTokenGenerator.GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtTokenGenerator.RefreshTokenExpiryDays);

        var refreshTokenEntity = RefreshToken.Create(
            refreshTokenHash,
            existingIdentity.Id,
            refreshTokenExpiry
        );

        _unitOfWork.RefreshTokens.Add(refreshTokenEntity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CompleteOAuthCallbackResult(
            existingIdentity.Id,
            profile.Id,
            existingIdentity.Email,
            profile.Name,
            profile.Handle,
            profile.Bio,
            profile.AvatarUrl,
            existingIdentity.Role,
            accessToken,
            refreshToken,
            IsNewUser: false);
    }

    private async Task<CompleteOAuthCallbackResult> HandleNewUserRegistrationAsync(
        OAuthProvider provider,
        OAuthUserInfo userInfo,
        OAuthTokenResponse tokenResponse,
        CancellationToken cancellationToken)
    {
        Identity identity;
        Profile profile;

        await using var transaction = await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        try
        {
            identity = Identity.CreateFromOAuth(userInfo.Email, userInfo.EmailVerified);
            _unitOfWork.Identities.Register(identity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var handle = await GenerateUniqueHandleAsync(
                userInfo.Name ?? userInfo.Email.Split('@')[0],
                cancellationToken);

            profile = Profile.Create(
                identity.Id,
                userInfo.Name ?? userInfo.Email.Split('@')[0],
                handle);

            // Update avatar URL if provided by OAuth provider
            if (!string.IsNullOrWhiteSpace(userInfo.Picture))
            {
                profile.UpdateProfile(null, null, userInfo.Picture);
            }

            _unitOfWork.Profiles.Create(profile);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var externalLogin = ExternalLogin.Create(
                identity.Id,
                provider,
                userInfo.ProviderUserId,
                userInfo.Email,
                userInfo.Name,
                userInfo.Picture,
                tokenResponse.AccessToken,
                tokenResponse.RefreshToken,
                DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn));

            _unitOfWork.ExternalLogins.Add(externalLogin);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(identity, profile);

        // Generate refresh token and store in database
        var (refreshToken, refreshTokenHash) = JwtTokenGenerator.GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtTokenGenerator.RefreshTokenExpiryDays);

        var refreshTokenEntity = RefreshToken.Create(
            refreshTokenHash,
            identity.Id,
            refreshTokenExpiry
        );

        _unitOfWork.RefreshTokens.Add(refreshTokenEntity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CompleteOAuthCallbackResult(
            identity.Id,
            profile.Id,
            identity.Email,
            profile.Name,
            profile.Handle,
            profile.Bio,
            profile.AvatarUrl,
            identity.Role,
            accessToken,
            refreshToken,
            IsNewUser: true);
    }

    private async Task<string> GenerateUniqueHandleAsync(
        string baseName,
        CancellationToken cancellationToken)
    {
        var baseHandle = new string(baseName
            .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
            .ToArray())
            .Replace(" ", "_")
            .ToLowerInvariant();

        if (baseHandle.Length < 3)
        {
            baseHandle = $"user_{baseHandle}";
        }

        var handleExists = await _unitOfWork.Profiles.ExistsByHandleAsync(
            baseHandle,
            cancellationToken);

        if (!handleExists)
        {
            return baseHandle;
        }

        for (var i = 1; i <= 999; i++)
        {
            var candidate = $"{baseHandle}{i}";
            var exists = await _unitOfWork.Profiles.ExistsByHandleAsync(
                candidate,
                cancellationToken);

            if (!exists)
            {
                return candidate;
            }
        }

        return $"{baseHandle}_{DateTime.UtcNow.Ticks}";
    }
}
