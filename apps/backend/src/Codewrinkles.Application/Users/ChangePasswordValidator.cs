using Kommand;

namespace Codewrinkles.Application.Users;

public sealed class ChangePasswordValidator : IValidator<ChangePasswordCommand>
{
    private List<ValidationError> _errors = null!;

    public Task<ValidationResult> ValidateAsync(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        _errors = new List<ValidationError>();

        // Current password validation
        ValidateCurrentPassword(request.CurrentPassword);

        // New password validation
        ValidateNewPassword(request.NewPassword);

        return Task.FromResult(_errors.Count > 0
            ? ValidationResult.Failure(_errors)
            : ValidationResult.Success());
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
}
