using Codewrinkles.Application.Common.Interfaces;
using Kommand;

namespace Codewrinkles.Application.Users;

public sealed class UploadAvatarValidator : IValidator<UploadAvatarCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private List<ValidationError> _errors = null!;

    public UploadAvatarValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ValidationResult> ValidateAsync(
        UploadAvatarCommand request,
        CancellationToken cancellationToken)
    {
        _errors = [];

        // Input format validation
        ValidateProfileId(request.ProfileId);
        ValidateImageStream(request.ImageStream);

        // If basic validation fails, return early
        if (_errors.Count > 0)
        {
            return ValidationResult.Failure(_errors);
        }

        // Application-level validation (requires database checks)
        await ValidateProfileExistsAsync(request.ProfileId, cancellationToken);

        return _errors.Count > 0
            ? ValidationResult.Failure(_errors)
            : ValidationResult.Success();
    }

    private void ValidateProfileId(Guid profileId)
    {
        if (profileId == Guid.Empty)
        {
            _errors.Add(new ValidationError(
                nameof(UploadAvatarCommand.ProfileId),
                "Profile ID is required"));
        }
    }

    private void ValidateImageStream(Stream? imageStream)
    {
        if (imageStream is null || imageStream.Length == 0)
        {
            _errors.Add(new ValidationError(
                nameof(UploadAvatarCommand.ImageStream),
                "Image file is required"));
        }
    }

    private async Task ValidateProfileExistsAsync(
        Guid profileId,
        CancellationToken cancellationToken)
    {
        var profile = await _unitOfWork.Profiles.FindByIdAsync(
            profileId,
            cancellationToken);

        if (profile is null)
        {
            throw new ProfileNotFoundException(profileId);
        }
    }
}
