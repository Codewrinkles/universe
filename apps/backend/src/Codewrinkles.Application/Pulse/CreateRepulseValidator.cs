using Codewrinkles.Application.Common.Exceptions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse.Exceptions;
using Kommand;

namespace Codewrinkles.Application.Pulse;

public sealed class CreateRepulseValidator : IValidator<CreateRepulseCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private List<ValidationError> _errors = [];

    public CreateRepulseValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ValidationResult> ValidateAsync(
        CreateRepulseCommand request,
        CancellationToken cancellationToken)
    {
        _errors = [];

        // 1. Validate content
        ValidateContent(request.Content);

        // If basic validation fails, return early
        if (_errors.Count > 0)
        {
            return ValidationResult.Failure(_errors);
        }

        // 2. Validate author exists
        await ValidateAuthorExistsAsync(request.AuthorId, cancellationToken);

        // 3. Validate repulsed pulse exists and is not deleted
        await ValidateRepulsedPulseAsync(request.RepulsedPulseId, cancellationToken);

        return _errors.Count > 0
            ? ValidationResult.Failure(_errors)
            : ValidationResult.Success();
    }

    private void ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            _errors.Add(new ValidationError(
                nameof(CreateRepulseCommand.Content),
                "Repulse content cannot be empty"));
            return;
        }

        // Normalize newlines (FormData converts \n to \r\n per HTML spec)
        var normalizedContent = content.Replace("\r\n", "\n").Replace("\r", "\n");
        var trimmedContent = normalizedContent.Trim();
        if (trimmedContent.Length > Domain.Pulse.Pulse.MaxContentLength)
        {
            _errors.Add(new ValidationError(
                nameof(CreateRepulseCommand.Content),
                $"Repulse content cannot exceed {Domain.Pulse.Pulse.MaxContentLength} characters"));
        }
    }

    private async Task ValidateAuthorExistsAsync(Guid authorId, CancellationToken cancellationToken)
    {
        var profile = await _unitOfWork.Profiles.FindByIdAsync(authorId, cancellationToken);
        if (profile is null)
        {
            throw new AuthorNotFoundException(authorId);
        }
    }

    private async Task ValidateRepulsedPulseAsync(Guid repulsedPulseId, CancellationToken cancellationToken)
    {
        var repulsedPulse = await _unitOfWork.Pulses.FindByIdAsync(repulsedPulseId, cancellationToken);

        if (repulsedPulse is null)
        {
            throw new PulseNotFoundException(repulsedPulseId);
        }

        if (repulsedPulse.IsDeleted)
        {
            throw new PulseNotFoundException(repulsedPulseId);
        }

        // Business rule: Users CAN re-pulse a re-pulse (nested quoting allowed)
        // Business rule: Users CAN re-pulse their own pulses
        // No additional restrictions needed here
    }
}
