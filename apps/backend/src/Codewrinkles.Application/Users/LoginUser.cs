using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Identity;

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
        // 1. Find identity by email (includes Profile via eager loading)
        var identity = await _unitOfWork.Identities.FindByEmailWithProfileAsync(
            command.Email,
            cancellationToken);

        if (identity is null)
        {
            throw new InvalidCredentialsException();
        }

        // 2. Check if account is active
        if (!identity.IsActive)
        {
            throw new AccountSuspendedException();
        }

        // 3. Check if account is locked out
        if (identity.IsLockedOut())
        {
            throw new AccountLockedException(identity.LockedUntil!.Value);
        }

        // 4. Verify password
        if (!_passwordHasher.VerifyPassword(command.Password, identity.PasswordHash))
        {
            // Record failed login attempt
            identity.RecordFailedLogin();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw new InvalidCredentialsException();
        }

        // 5. Record successful login
        identity.RecordSuccessfulLogin();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Generate JWT tokens
        var profile = identity.Profile;
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(identity, profile);
        var refreshToken = JwtTokenGenerator.GenerateRefreshToken(identity);

        // 7. Return result
        return new LoginUserResult(
            IdentityId: identity.Id,
            ProfileId: profile.Id,
            Email: identity.Email,
            Name: profile.Name,
            Handle: profile.Handle,
            AccessToken: accessToken,
            RefreshToken: refreshToken
        );
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
