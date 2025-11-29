using System.Data;
using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Identity;

namespace Codewrinkles.Application.Users;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string Name,
    string? Handle = null
) : ICommand<RegisterUserResult>;

public sealed record RegisterUserResult(
    Guid IdentityId,
    Guid ProfileId,
    string Email,
    string Name,
    string? Handle,
    UserRole Role,
    string AccessToken,
    string RefreshToken
);

public sealed class RegisterUserCommandHandler
    : ICommandHandler<RegisterUserCommand, RegisterUserResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtTokenGenerator _jwtTokenGenerator;

    public RegisterUserCommandHandler(
        IUnitOfWork unitOfWork,
        PasswordHasher passwordHasher,
        JwtTokenGenerator jwtTokenGenerator)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<RegisterUserResult> HandleAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Hash password
        var passwordHash = _passwordHasher.HashPassword(command.Password);

        // 2. Create Identity and Profile atomically
        Identity identity;
        Profile profile;

        await using var transaction = await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        try
        {
            // Create Identity
            identity = Identity.Create(
                email: command.Email,
                passwordHash: passwordHash);

            _unitOfWork.Identities.Register(identity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Create Profile linked to Identity
            profile = Profile.Create(
                identityId: identity.Id,
                name: command.Name,
                handle: command.Handle);

            _unitOfWork.Profiles.Create(profile);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Commit transaction - both Identity and Profile created
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            // Rollback on any error - neither Identity nor Profile created
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        // 3. Generate JWT tokens (after successful commit)
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(identity, profile);
        var refreshToken = JwtTokenGenerator.GenerateRefreshToken(identity);

        // 4. Return result
        return new RegisterUserResult(
            IdentityId: identity.Id,
            ProfileId: profile.Id,
            Email: identity.Email,
            Name: profile.Name,
            Handle: profile.Handle,
            Role: identity.Role,
            AccessToken: accessToken,
            RefreshToken: refreshToken
        );
    }
}
