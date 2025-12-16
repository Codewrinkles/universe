using Codewrinkles.Application.Common.Exceptions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Kommand;

namespace Codewrinkles.Application.Nova;

public sealed class SendMessageValidator : IValidator<SendMessageCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private List<ValidationError> _errors = null!;

    public SendMessageValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ValidationResult> ValidateAsync(
        SendMessageCommand command,
        CancellationToken cancellationToken)
    {
        _errors = [];

        // Input validation
        ValidateMessage(command.Message);

        // If basic validation fails, return early
        if (_errors.Count > 0)
        {
            return ValidationResult.Failure(_errors);
        }

        // Business rule validation - ensure profile exists
        await ValidateProfileExistsAsync(command.ProfileId, cancellationToken);

        return _errors.Count > 0
            ? ValidationResult.Failure(_errors)
            : ValidationResult.Success();
    }

    private void ValidateMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            _errors.Add(new ValidationError(
                nameof(SendMessageCommand.Message),
                "Message content is required"));
            return;
        }

        var trimmedMessage = message.Trim();
        if (trimmedMessage.Length > Message.MaxContentLength)
        {
            _errors.Add(new ValidationError(
                nameof(SendMessageCommand.Message),
                $"Message cannot exceed {Message.MaxContentLength} characters"));
        }
    }

    private async Task ValidateProfileExistsAsync(Guid profileId, CancellationToken cancellationToken)
    {
        var profile = await _unitOfWork.Profiles.FindByIdAsync(profileId, cancellationToken);
        if (profile is null)
        {
            throw new AuthorNotFoundException(profileId);
        }
    }
}
