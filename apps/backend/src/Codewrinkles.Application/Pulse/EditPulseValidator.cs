using Codewrinkles.Application.Common.Exceptions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse.Exceptions;
using Kommand;

namespace Codewrinkles.Application.Pulse;

public sealed class EditPulseValidator : IValidator<EditPulseCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public EditPulseValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ValidationResult> ValidateAsync(
        EditPulseCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate content not empty
        if (string.IsNullOrWhiteSpace(request.NewContent))
        {
            return ValidationResult.Failure([
                new ValidationError("NewContent", "Content cannot be empty")
            ]);
        }

        // 2. Validate content length (normalize first for accurate counting)
        var normalizedContent = request.NewContent
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Trim();

        if (normalizedContent.Length > Domain.Pulse.Pulse.MaxContentLength)
        {
            return ValidationResult.Failure([
                new ValidationError(
                    "NewContent",
                    $"Content cannot exceed {Domain.Pulse.Pulse.MaxContentLength} characters. Current length: {normalizedContent.Length}")
            ]);
        }

        // 3. Check pulse exists
        var pulse = await _unitOfWork.Pulses.FindByIdAsync(request.PulseId, cancellationToken);

        if (pulse is null)
        {
            throw new PulseNotFoundException(request.PulseId);
        }

        // 4. Check pulse is not deleted
        if (pulse.IsDeleted)
        {
            throw new PulseAlreadyDeletedException(request.PulseId);
        }

        // 5. Verify user is the author
        if (pulse.AuthorId != request.ProfileId)
        {
            throw new UnauthorizedPulseAccessException(request.PulseId, request.ProfileId);
        }

        return ValidationResult.Success();
    }
}
