using Codewrinkles.Application.Common.Interfaces;
using Kommand;

namespace Codewrinkles.Application.Pulse;

public sealed class BookmarkPulseValidator : IValidator<BookmarkPulseCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly List<ValidationError> _errors = [];

    public BookmarkPulseValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ValidationResult> ValidateAsync(
        BookmarkPulseCommand request,
        CancellationToken cancellationToken)
    {
        // Check if pulse exists
        var pulse = await _unitOfWork.Pulses.FindByIdAsync(request.PulseId, cancellationToken);
        if (pulse is null)
        {
            _errors.Add(new ValidationError(
                PropertyName: nameof(request.PulseId),
                ErrorMessage: "Pulse not found"));
            return ValidationResult.Failure(_errors);
        }

        // Check if pulse is deleted
        if (pulse.IsDeleted)
        {
            _errors.Add(new ValidationError(
                PropertyName: nameof(request.PulseId),
                ErrorMessage: "Cannot bookmark deleted pulse"));
            return ValidationResult.Failure(_errors);
        }

        // Check if already bookmarked
        var isBookmarked = await _unitOfWork.Bookmarks.IsBookmarkedAsync(
            request.ProfileId,
            request.PulseId,
            cancellationToken);

        if (isBookmarked)
        {
            _errors.Add(new ValidationError(
                PropertyName: nameof(request.PulseId),
                ErrorMessage: "Pulse is already bookmarked"));
            return ValidationResult.Failure(_errors);
        }

        return ValidationResult.Success();
    }
}
