using System.Text.RegularExpressions;
using Codewrinkles.Application.Common.Interfaces;
using Kommand;

namespace Codewrinkles.Application.Users;

public sealed partial class LoginUserValidator : IValidator<LoginUserCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private List<ValidationError> _errors = null!;

    public LoginUserValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ValidationResult> ValidateAsync(
        LoginUserCommand request,
        CancellationToken cancellationToken)
    {
        _errors = [];

        // Input format validation
        ValidateEmail(request.Email);
        ValidatePassword(request.Password);

        // If basic validation fails, return early
        if (_errors.Count > 0)
        {
            return ValidationResult.Failure(_errors);
        }

        // Application-level validation (requires database checks)
        await ValidateIdentityAsync(request.Email, cancellationToken);

        return _errors.Count > 0
            ? ValidationResult.Failure(_errors)
            : ValidationResult.Success();
    }

    private void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _errors.Add(new ValidationError(
                nameof(LoginUserCommand.Email),
                "Email is required"));
            return;
        }

        if (!EmailRegex().IsMatch(email))
        {
            _errors.Add(new ValidationError(
                nameof(LoginUserCommand.Email),
                "Invalid email format"));
        }
    }

    private void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            _errors.Add(new ValidationError(
                nameof(LoginUserCommand.Password),
                "Password is required"));
        }
    }

    private async Task ValidateIdentityAsync(string email, CancellationToken cancellationToken)
    {
        // Find identity by email
        var identity = await _unitOfWork.Identities.FindByEmailAsync(
            email,
            cancellationToken);

        // Check if identity exists
        // Use generic error to prevent email enumeration attacks
        if (identity is null)
        {
            throw new InvalidCredentialsException();
        }

        // Check if account is active
        if (!identity.IsActive)
        {
            throw new AccountSuspendedException();
        }

        // Check if account is locked out
        if (identity.IsLockedOut())
        {
            throw new AccountLockedException(identity.LockedUntil!.Value);
        }
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();
}
