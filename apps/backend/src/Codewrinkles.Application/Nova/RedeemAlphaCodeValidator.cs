using Codewrinkles.Application.Common.Interfaces;
using Kommand;

namespace Codewrinkles.Application.Nova;

public sealed class RedeemAlphaCodeValidator : IValidator<RedeemAlphaCodeCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private List<ValidationError> _errors = null!;

    public RedeemAlphaCodeValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ValidationResult> ValidateAsync(
        RedeemAlphaCodeCommand command,
        CancellationToken cancellationToken)
    {
        _errors = [];

        // Basic validation
        ValidateCode(command.Code);

        if (_errors.Count > 0)
        {
            return ValidationResult.Failure(_errors);
        }

        // Business rule validation
        await ValidateProfileAsync(command.ProfileId, cancellationToken);
        await ValidateCodeExistsAndNotRedeemedAsync(command.Code, cancellationToken);

        return _errors.Count > 0
            ? ValidationResult.Failure(_errors)
            : ValidationResult.Success();
    }

    private void ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            _errors.Add(new ValidationError(nameof(RedeemAlphaCodeCommand.Code), "Invite code is required"));
        }
    }

    private async Task ValidateProfileAsync(Guid profileId, CancellationToken cancellationToken)
    {
        var profile = await _unitOfWork.Profiles.FindByIdAsync(profileId, cancellationToken);

        if (profile is null)
        {
            throw new ProfileNotFoundException(profileId);
        }

        // Check if user already has Nova access
        if (profile.HasNovaAccess)
        {
            throw new AlreadyHasNovaAccessException();
        }
    }

    private async Task ValidateCodeExistsAndNotRedeemedAsync(string code, CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var application = await _unitOfWork.AlphaApplications.FindByInviteCodeAsync(normalizedCode, cancellationToken);

        if (application is null)
        {
            throw new InvalidAlphaCodeException();
        }

        if (application.InviteCodeRedeemed)
        {
            throw new InvalidAlphaCodeException();
        }
    }
}
