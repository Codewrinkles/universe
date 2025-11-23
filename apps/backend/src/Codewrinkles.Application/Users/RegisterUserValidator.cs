using System.Text.RegularExpressions;
using Codewrinkles.Application.Common.Interfaces;
using Kommand;

namespace Codewrinkles.Application.Users;

public sealed class RegisterUserValidator : IValidator<RegisterUserCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private List<ValidationError> _errors = null!;

    public RegisterUserValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ValidationResult> ValidateAsync(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        _errors = new List<ValidationError>();

        // Email validation
        ValidateEmail(request.Email);

        // Password validation
        ValidatePassword(request.Password);

        // Name validation
        ValidateName(request.Name);

        // Handle validation (if provided)
        if (!string.IsNullOrWhiteSpace(request.Handle))
        {
            ValidateHandle(request.Handle);
        }

        // Async validation - check if email already exists
        await ValidateEmailUniquenessAsync(request.Email, cancellationToken);

        // Async validation - check if handle already exists (if provided)
        if (!string.IsNullOrWhiteSpace(request.Handle))
        {
            await ValidateHandleUniquenessAsync(request.Handle, cancellationToken);
        }

        return _errors.Count > 0
            ? ValidationResult.Failure(_errors)
            : ValidationResult.Success();
    }

    private void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _errors.Add(new ValidationError(
                nameof(RegisterUserCommand.Email),
                "Email is required"));
            return;
        }

        var emailRegex = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.IgnoreCase);

        if (!emailRegex.IsMatch(email))
        {
            _errors.Add(new ValidationError(
                nameof(RegisterUserCommand.Email),
                "Invalid email format"));
        }
    }

    private void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            _errors.Add(new ValidationError(
                nameof(RegisterUserCommand.Password),
                "Password is required"));
            return;
        }

        if (password.Length < 8)
        {
            _errors.Add(new ValidationError(
                nameof(RegisterUserCommand.Password),
                "Password must be at least 8 characters"));
        }

        var hasUpperCase = password.Any(char.IsUpper);
        var hasLowerCase = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecialChar = password.Any(c => !char.IsLetterOrDigit(c));

        if (!hasUpperCase || !hasLowerCase || !hasDigit || !hasSpecialChar)
        {
            _errors.Add(new ValidationError(
                nameof(RegisterUserCommand.Password),
                "Password must contain uppercase, lowercase, number, and special character"));
        }
    }

    private void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _errors.Add(new ValidationError(
                nameof(RegisterUserCommand.Name),
                "Name is required"));
            return;
        }

        if (name.Length < 2 || name.Length > 100)
        {
            _errors.Add(new ValidationError(
                nameof(RegisterUserCommand.Name),
                "Name must be 2-100 characters"));
        }
    }

    private void ValidateHandle(string handle)
    {
        if (handle.Length < 3 || handle.Length > 50)
        {
            _errors.Add(new ValidationError(
                nameof(RegisterUserCommand.Handle),
                "Handle must be 3-50 characters"));
        }

        var handleRegex = new Regex(@"^[a-zA-Z0-9_-]+$");
        if (!handleRegex.IsMatch(handle))
        {
            _errors.Add(new ValidationError(
                nameof(RegisterUserCommand.Handle),
                "Handle can only contain letters, numbers, underscores, and hyphens"));
        }
    }

    private async Task ValidateEmailUniquenessAsync(
        string email,
        CancellationToken cancellationToken)
    {
        var emailExists = await _unitOfWork.Identities.ExistsByEmailAsync(
            email,
            cancellationToken);

        if (emailExists)
        {
            _errors.Add(new ValidationError(
                nameof(RegisterUserCommand.Email),
                "Email already registered"));
        }
    }

    private async Task ValidateHandleUniquenessAsync(
        string handle,
        CancellationToken cancellationToken)
    {
        var handleExists = await _unitOfWork.Profiles.ExistsByHandleAsync(
            handle,
            cancellationToken);

        if (handleExists)
        {
            _errors.Add(new ValidationError(
                nameof(RegisterUserCommand.Handle),
                "Handle already taken"));
        }
    }
}
