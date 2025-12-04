using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse.Exceptions;
using Kommand;

namespace Codewrinkles.Application.Pulse;

public sealed class CreateReplyValidator : IValidator<CreateReplyCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private List<ValidationError> _errors = [];

    public CreateReplyValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ValidationResult> ValidateAsync(
        CreateReplyCommand request,
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

        // 2. Validate parent pulse exists and is not deleted
        await ValidateParentPulseAsync(request.ParentPulseId, cancellationToken);

        return _errors.Count > 0
            ? ValidationResult.Failure(_errors)
            : ValidationResult.Success();
    }

    private void ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            _errors.Add(new ValidationError(
                nameof(CreateReplyCommand.Content),
                "Reply content cannot be empty"));
            return;
        }

        // Normalize newlines (FormData converts \n to \r\n per HTML spec)
        var normalizedContent = content.Replace("\r\n", "\n").Replace("\r", "\n");
        var trimmedContent = normalizedContent.Trim();
        if (trimmedContent.Length > Domain.Pulse.Pulse.MaxContentLength)
        {
            _errors.Add(new ValidationError(
                nameof(CreateReplyCommand.Content),
                $"Reply content cannot exceed {Domain.Pulse.Pulse.MaxContentLength} characters"));
        }
    }

    private async Task ValidateParentPulseAsync(Guid parentPulseId, CancellationToken cancellationToken)
    {
        var parentPulse = await _unitOfWork.Pulses.FindByIdAsync(parentPulseId, cancellationToken);

        if (parentPulse is null)
        {
            throw new PulseNotFoundException(parentPulseId);
        }

        if (parentPulse.IsDeleted)
        {
            throw new PulseNotFoundException(parentPulseId);
        }

        // Prevent nested replies: replies can only be made to original pulses or repulses, not to other replies
        if (parentPulse.Type == Domain.Pulse.PulseType.Reply)
        {
            _errors.Add(new ValidationError(
                nameof(CreateReplyCommand.ParentPulseId),
                "Cannot reply to a reply. Only one level of nesting is supported."));
        }
    }
}
