using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Identity;
using Codewrinkles.Telemetry;

namespace Codewrinkles.Application.Users;

public sealed record LoginUserCommand(
    string Email,
    string Password
) : ICommand<LoginUserResult>;

public sealed record LoginUserResult(
    Guid IdentityId,
    Guid ProfileId,
    string Email,
    string Name,
    string? Handle,
    string? Bio,
    string? AvatarUrl,
    UserRole Role,
    string AccessToken,
    string RefreshToken
);

public sealed class LoginUserCommandHandler
    : ICommandHandler<LoginUserCommand, LoginUserResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtTokenGenerator _jwtTokenGenerator;

    public LoginUserCommandHandler(
        IUnitOfWork unitOfWork,
        PasswordHasher passwordHasher,
        JwtTokenGenerator jwtTokenGenerator)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginUserResult> HandleAsync(
        LoginUserCommand command,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Authentication.Login);
        activity?.SetEmailDomain(command.Email);

        try
        {
            // Validator has already confirmed:
            // - Identity exists
            // - Account is active
            // - Account is not locked out

            // 1. Find identity by email (includes Profile via eager loading)
            // Identity is guaranteed to exist after validation
            var identity = (await _unitOfWork.Identities.FindByEmailWithProfileAsync(
                command.Email,
                cancellationToken))!;

            activity?.SetUserContext(identity.Id, identity.Profile.Id);

            // 2. Verify password (has side effect: records failed attempts)
            if (!_passwordHasher.VerifyPassword(command.Password, identity.PasswordHash))
            {
                // Record failed login attempt
                identity.RecordFailedLogin();
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                AppMetrics.RecordLoginAttempt(success: false, failureReason: "invalid_password");
                activity?.SetSuccess(false);
                throw new InvalidCredentialsException();
            }

            // 3. Record successful login
            identity.RecordSuccessfulLogin();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 4. Generate JWT tokens
            var profile = identity.Profile;
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

            // Record metrics
            AppMetrics.RecordLoginAttempt(success: true);
            AppMetrics.RecordUserLoggedIn(authMethod: "password");
            activity?.SetSuccess(true);

            // 5. Return result
            return new LoginUserResult(
                IdentityId: identity.Id,
                ProfileId: profile.Id,
                Email: identity.Email,
                Name: profile.Name,
                Handle: profile.Handle,
                Bio: profile.Bio,
                AvatarUrl: profile.AvatarUrl,
                Role: identity.Role,
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
/// Thrown when email or password is invalid.
/// Generic message to prevent email enumeration attacks.
/// </summary>
public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("Invalid email or password") { }
}

/// <summary>
/// Thrown when account is suspended.
/// </summary>
public sealed class AccountSuspendedException : Exception
{
    public AccountSuspendedException()
        : base("This account has been suspended") { }
}

/// <summary>
/// Thrown when account is locked due to too many failed login attempts.
/// </summary>
public sealed class AccountLockedException : Exception
{
    public DateTime LockedUntil { get; }

    public AccountLockedException(DateTime lockedUntil)
        : base($"Account is locked. Please try again after {lockedUntil:HH:mm} UTC")
    {
        LockedUntil = lockedUntil;
    }
}
