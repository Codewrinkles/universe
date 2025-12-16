using Codewrinkles.Application.Common.Exceptions;
using Codewrinkles.Application.Common.Interfaces;
using Kommand;

namespace Codewrinkles.Application.Nova;

public sealed class DeleteConversationValidator : IValidator<DeleteConversationCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private List<ValidationError> _errors = null!;

    public DeleteConversationValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ValidationResult> ValidateAsync(
        DeleteConversationCommand command,
        CancellationToken cancellationToken)
    {
        _errors = [];

        // Business rule validation - ensure profile exists
        await ValidateProfileExistsAsync(command.ProfileId, cancellationToken);

        return _errors.Count > 0
            ? ValidationResult.Failure(_errors)
            : ValidationResult.Success();
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
