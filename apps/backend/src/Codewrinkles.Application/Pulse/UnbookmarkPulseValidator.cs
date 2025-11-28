using Codewrinkles.Application.Common.Interfaces;
using Kommand;

namespace Codewrinkles.Application.Pulse;

public sealed class UnbookmarkPulseValidator : IValidator<UnbookmarkPulseCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly List<ValidationError> _errors = [];

    public UnbookmarkPulseValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ValidationResult> ValidateAsync(
        UnbookmarkPulseCommand request,
        CancellationToken cancellationToken)
    {
        // Check if bookmark exists
        var bookmark = await _unitOfWork.Bookmarks.FindByProfileAndPulseAsync(
            request.ProfileId,
            request.PulseId,
            cancellationToken);

        if (bookmark is null)
        {
            _errors.Add(new ValidationError(
                PropertyName: nameof(request.PulseId),
                ErrorMessage: "Bookmark not found"));
            return ValidationResult.Failure(_errors);
        }

        return ValidationResult.Success();
    }
}
