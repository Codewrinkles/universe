using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Identity;

namespace Codewrinkles.Application.Users;

public sealed record ChangePasswordCommand(
    Guid IdentityId,
    string CurrentPassword,
    string NewPassword
) : ICommand<ChangePasswordResult>;

public sealed record ChangePasswordResult(
    bool Success
);

public sealed class ChangePasswordCommandHandler
    : ICommandHandler<ChangePasswordCommand, ChangePasswordResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(
        IUnitOfWork unitOfWork,
        PasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<ChangePasswordResult> HandleAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        // Validator has already confirmed:
        // - Identity exists
        // - Current password is correct

        // 1. Find identity (guaranteed to exist after validation)
        var identity = (await _unitOfWork.Identities.FindByIdAsync(
            command.IdentityId,
            cancellationToken))!;

        // 2. Hash new password and update
        var newPasswordHash = _passwordHasher.HashPassword(command.NewPassword);
        identity.ChangePassword(newPasswordHash);

        // 3. Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ChangePasswordResult(Success: true);
    }
}

/// <summary>
/// Thrown when identity is not found.
/// </summary>
public sealed class IdentityNotFoundException : Exception
{
    public IdentityNotFoundException()
        : base("Identity not found") { }
}

/// <summary>
/// Thrown when current password verification fails.
/// </summary>
public sealed class CurrentPasswordInvalidException : Exception
{
    public CurrentPasswordInvalidException()
        : base("Current password is incorrect") { }
}
