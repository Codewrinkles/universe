using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse.Exceptions;
using Kommand;

namespace Codewrinkles.Application.Pulse;

public sealed class CreatePulseValidator : IValidator<CreatePulseCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private List<ValidationError> _errors = null!;

    public CreatePulseValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ValidationResult> ValidateAsync(
        CreatePulseCommand command,
        CancellationToken cancellationToken)
    {
        _errors = [];

        // Input validation
        ValidateContent(command.Content);

        // If basic validation fails, return early
        if (_errors.Count > 0)
        {
            return ValidationResult.Failure(_errors);
        }

        // Business rule validation - ensure author exists
        await ValidateAuthorExistsAsync(command.AuthorId, cancellationToken);

        return _errors.Count > 0
            ? ValidationResult.Failure(_errors)
            : ValidationResult.Success();
    }

    private void ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            _errors.Add(new ValidationError(
                nameof(CreatePulseCommand.Content),
                "Pulse content is required"));
            return;
        }

        var trimmedContent = content.Trim();
        if (trimmedContent.Length > Domain.Pulse.Pulse.MaxContentLength)
        {
            _errors.Add(new ValidationError(
                nameof(CreatePulseCommand.Content),
                $"Pulse content cannot exceed {Domain.Pulse.Pulse.MaxContentLength} characters"));
        }
    }

    private async Task ValidateAuthorExistsAsync(Guid authorId, CancellationToken cancellationToken)
    {
        var profile = await _unitOfWork.Profiles.FindByIdAsync(authorId, cancellationToken);
        if (profile is null)
        {
            throw new InvalidOperationException($"Profile with ID '{authorId}' not found");
        }
    }
}
