using Codewrinkles.Domain.Identity;
using Kommand;

namespace Codewrinkles.Application.Users;

public sealed class CompleteOAuthCallbackValidator : IValidator<CompleteOAuthCallbackCommand>
{
    private List<ValidationError> _errors = null!;

    public Task<ValidationResult> ValidateAsync(
        CompleteOAuthCallbackCommand request,
        CancellationToken cancellationToken)
    {
        _errors = [];

        ValidateProvider(request.Provider);
        ValidateCode(request.Code);
        ValidateState(request.State);
        ValidateRedirectUri(request.RedirectUri);

        if (_errors.Count > 0)
        {
            return Task.FromResult(ValidationResult.Failure(_errors));
        }

        return Task.FromResult(ValidationResult.Success());
    }

    private void ValidateProvider(OAuthProvider provider)
    {
        if (!Enum.IsDefined(typeof(OAuthProvider), provider))
        {
            _errors.Add(new ValidationError(
                nameof(CompleteOAuthCallbackCommand.Provider),
                "Invalid OAuth provider"));
        }
    }

    private void ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            _errors.Add(new ValidationError(
                nameof(CompleteOAuthCallbackCommand.Code),
                "Authorization code is required"));
        }
    }

    private void ValidateState(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            _errors.Add(new ValidationError(
                nameof(CompleteOAuthCallbackCommand.State),
                "State parameter is required"));
        }
    }

    private void ValidateRedirectUri(string redirectUri)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            _errors.Add(new ValidationError(
                nameof(CompleteOAuthCallbackCommand.RedirectUri),
                "Redirect URI is required"));
            return;
        }

        if (!Uri.IsWellFormedUriString(redirectUri, UriKind.Absolute))
        {
            _errors.Add(new ValidationError(
                nameof(CompleteOAuthCallbackCommand.RedirectUri),
                "Redirect URI must be a valid absolute URL"));
        }
    }
}
