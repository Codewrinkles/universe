using System.Text.RegularExpressions;
using Kommand;

namespace Codewrinkles.Application.Users;

public sealed partial class UpdateProfileValidator : IValidator<UpdateProfileCommand>
{
    private List<ValidationError> _errors = null!;

    public Task<ValidationResult> ValidateAsync(
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        _errors = [];

        // ProfileId validation
        if (request.ProfileId == Guid.Empty)
        {
            _errors.Add(new ValidationError(
                nameof(UpdateProfileCommand.ProfileId),
                "Profile ID is required"));
        }

        // Name validation
        ValidateName(request.Name);

        // Handle validation (if provided)
        if (!string.IsNullOrWhiteSpace(request.Handle))
        {
            ValidateHandle(request.Handle);
        }

        // Bio validation (if provided)
        if (!string.IsNullOrWhiteSpace(request.Bio))
        {
            ValidateBio(request.Bio);
        }

        return Task.FromResult(_errors.Count > 0
            ? ValidationResult.Failure(_errors)
            : ValidationResult.Success());
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

    [GeneratedRegex(@"^[a-zA-Z0-9_-]+$")]
    private static partial Regex HandleRegex();
}
