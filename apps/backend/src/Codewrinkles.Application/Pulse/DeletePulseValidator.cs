using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse.Exceptions;
using Kommand;

namespace Codewrinkles.Application.Pulse;

public sealed class DeletePulseValidator : IValidator<DeletePulseCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeletePulseValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ValidationResult> ValidateAsync(
        DeletePulseCommand request,
        CancellationToken cancellationToken)
    {
        // Business rule validation - ensure pulse exists and user is the author
        var pulse = await _unitOfWork.Pulses.FindByIdAsync(request.PulseId, cancellationToken);

        if (pulse is null)
        {
            throw new PulseNotFoundException(request.PulseId);
        }

        if (pulse.IsDeleted)
        {
            throw new PulseAlreadyDeletedException(request.PulseId);
        }

        // Verify user is the author
        if (pulse.AuthorId != request.ProfileId)
        {
            throw new InvalidOperationException("You can only delete your own pulses");
        }

        return ValidationResult.Success();
    }
}
