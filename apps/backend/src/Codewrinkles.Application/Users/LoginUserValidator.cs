using System.Text.RegularExpressions;
using Kommand;

namespace Codewrinkles.Application.Users;

public sealed class LoginUserValidator : IValidator<LoginUserCommand>
{
    private List<ValidationError> _errors = null!;

    public Task<ValidationResult> ValidateAsync(
        LoginUserCommand request,
        CancellationToken cancellationToken)
    {
        _errors = new List<ValidationError>();

        ValidateEmail(request.Email);
        ValidatePassword(request.Password);

        return Task.FromResult(
            _errors.Count > 0
                ? ValidationResult.Failure(_errors)
                : ValidationResult.Success()
        );
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

        var emailRegex = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.IgnoreCase);

        if (!emailRegex.IsMatch(email))
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
}
