using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Identity;
using Kommand;

namespace Codewrinkles.Application.Users;

public sealed class ChangePasswordValidator : IValidator<ChangePasswordCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PasswordHasher _passwordHasher;
    private List<ValidationError> _errors = null!;

    public ChangePasswordValidator(IUnitOfWork unitOfWork, PasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<ValidationResult> ValidateAsync(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        _errors = [];

        // Input format validation
        ValidateIdentityId(request.IdentityId);
        ValidateCurrentPassword(request.CurrentPassword);
        ValidateNewPassword(request.NewPassword);

        // If basic validation fails, return early
        if (_errors.Count > 0)
        {
            return ValidationResult.Failure(_errors);
        }

        // Application-level validation (requires database checks)
        await ValidateIdentityAndPasswordAsync(
            request.IdentityId,
            request.CurrentPassword,
            cancellationToken);

        return _errors.Count > 0
            ? ValidationResult.Failure(_errors)
            : ValidationResult.Success();
    }

    private void ValidateIdentityId(Guid identityId)
    {
        if (identityId == Guid.Empty)
        {
            _errors.Add(new ValidationError(
                nameof(ChangePasswordCommand.IdentityId),
                "Identity ID is required"));
        }
    }

    private void ValidateCurrentPassword(string currentPassword)
    {
        if (string.IsNullOrWhiteSpace(currentPassword))
        {
            _errors.Add(new ValidationError(
                nameof(ChangePasswordCommand.CurrentPassword),
                "Current password is required"));
        }
    }

    private void ValidateNewPassword(string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            _errors.Add(new ValidationError(
                nameof(ChangePasswordCommand.NewPassword),
                "New password is required"));
            return;
        }

        if (newPassword.Length < 8)
        {
            _errors.Add(new ValidationError(
                nameof(ChangePasswordCommand.NewPassword),
                "Password must be at least 8 characters"));
        }

        var hasUpperCase = newPassword.Any(char.IsUpper);
        var hasLowerCase = newPassword.Any(char.IsLower);
        var hasDigit = newPassword.Any(char.IsDigit);
        var hasSpecialChar = newPassword.Any(c => !char.IsLetterOrDigit(c));

        if (!hasUpperCase || !hasLowerCase || !hasDigit || !hasSpecialChar)
        {
            _errors.Add(new ValidationError(
                nameof(ChangePasswordCommand.NewPassword),
                "Password must contain uppercase, lowercase, number, and special character"));
        }
    }

    private async Task ValidateIdentityAndPasswordAsync(
        Guid identityId,
        string currentPassword,
        CancellationToken cancellationToken)
    {
        // Check identity exists
        var identity = await _unitOfWork.Identities.FindByIdAsync(
            identityId,
            cancellationToken);

        if (identity is null)
        {
            throw new IdentityNotFoundException();
        }

        // Verify current password
        if (!_passwordHasher.VerifyPassword(currentPassword, identity.PasswordHash))
        {
            throw new CurrentPasswordInvalidException();
        }
    }
}
