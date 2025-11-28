using System.Text.RegularExpressions;
using Codewrinkles.Application.Common.Interfaces;
using Kommand;

namespace Codewrinkles.Application.Users;

public sealed partial class UpdateProfileValidator : IValidator<UpdateProfileCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private List<ValidationError> _errors = null!;

    public UpdateProfileValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ValidationResult> ValidateAsync(
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        _errors = [];

        // Input format validation
        ValidateProfileId(request.ProfileId);
        ValidateName(request.Name);

        if (!string.IsNullOrWhiteSpace(request.Handle))
        {
            ValidateHandle(request.Handle);
        }

        if (!string.IsNullOrWhiteSpace(request.Bio))
        {
            ValidateBio(request.Bio);
        }

        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            ValidateLocation(request.Location);
        }

        if (!string.IsNullOrWhiteSpace(request.WebsiteUrl))
        {
            ValidateWebsiteUrl(request.WebsiteUrl);
        }

        // If basic validation fails, return early
        if (_errors.Count > 0)
        {
            return ValidationResult.Failure(_errors);
        }

        // Application-level validation (requires database checks)
        await ValidateProfileExistsAsync(request.ProfileId, cancellationToken);
        await ValidateHandleUniquenessAsync(
            request.ProfileId,
            request.Handle,
            cancellationToken);

        return _errors.Count > 0
            ? ValidationResult.Failure(_errors)
            : ValidationResult.Success();
    }

    private void ValidateProfileId(Guid profileId)
    {
        if (profileId == Guid.Empty)
        {
            _errors.Add(new ValidationError(
                nameof(UpdateProfileCommand.ProfileId),
                "Profile ID is required"));
        }
    }

    private void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _errors.Add(new ValidationError(
                nameof(UpdateProfileCommand.Name),
                "Name is required"));
            return;
        }

        if (name.Length < 2 || name.Length > 100)
        {
            _errors.Add(new ValidationError(
                nameof(UpdateProfileCommand.Name),
                "Name must be 2-100 characters"));
        }
    }

    private void ValidateHandle(string handle)
    {
        if (handle.Length < 3 || handle.Length > 50)
        {
            _errors.Add(new ValidationError(
                nameof(UpdateProfileCommand.Handle),
                "Handle must be 3-50 characters"));
        }

        if (!HandleRegex().IsMatch(handle))
        {
            _errors.Add(new ValidationError(
                nameof(UpdateProfileCommand.Handle),
                "Handle can only contain letters, numbers, underscores, and hyphens"));
        }
    }

    private void ValidateBio(string bio)
    {
        if (bio.Length > 500)
        {
            _errors.Add(new ValidationError(
                nameof(UpdateProfileCommand.Bio),
                "Bio must be 500 characters or less"));
        }
    }

    private void ValidateLocation(string location)
    {
        if (location.Length > 100)
        {
            _errors.Add(new ValidationError(
                nameof(UpdateProfileCommand.Location),
                "Location must be 100 characters or less"));
        }
    }

    private void ValidateWebsiteUrl(string websiteUrl)
    {
        if (websiteUrl.Length > 500)
        {
            _errors.Add(new ValidationError(
                nameof(UpdateProfileCommand.WebsiteUrl),
                "Website URL must be 500 characters or less"));
        }

        // Basic URL format validation
        if (!Uri.TryCreate(websiteUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            _errors.Add(new ValidationError(
                nameof(UpdateProfileCommand.WebsiteUrl),
                "Website URL must be a valid HTTP or HTTPS URL"));
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

    private async Task ValidateHandleUniquenessAsync(
        Guid profileId,
        string? handle,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(handle))
        {
            return;
        }

        // Get current profile to check if handle is actually changing
        var currentProfile = await _unitOfWork.Profiles.FindByIdAsync(
            profileId,
            cancellationToken);

        if (currentProfile is null)
        {
            return; // Already handled by ValidateProfileExistsAsync
        }

        var normalizedNewHandle = handle.Trim().ToLowerInvariant();
        var currentHandle = currentProfile.Handle?.ToLowerInvariant();

        // Only check uniqueness if handle is actually changing
        if (normalizedNewHandle != currentHandle)
        {
            var handleExists = await _unitOfWork.Profiles.ExistsByHandleAsync(
                handle,
                cancellationToken);

            if (handleExists)
            {
                throw new HandleAlreadyTakenException(handle);
            }
        }
    }

    [GeneratedRegex(@"^[a-zA-Z0-9_-]+$")]
    private static partial Regex HandleRegex();
}
