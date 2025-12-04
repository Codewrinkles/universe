using Codewrinkles.Application.Common.Exceptions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse.Exceptions;
using Kommand;

namespace Codewrinkles.Application.Pulse;

public sealed class UnlikePulseValidator : IValidator<UnlikePulseCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private List<ValidationError> _errors = null!;

    public UnlikePulseValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ValidationResult> ValidateAsync(
        UnlikePulseCommand command,
        CancellationToken cancellationToken)
    {
        _errors = [];

        // Business rule validation
        await ValidatePulseExistsAndNotDeletedAsync(command.PulseId, cancellationToken);
        await ValidateUserHasLikedPulseAsync(command.PulseId, command.ProfileId, cancellationToken);

        return _errors.Count > 0
            ? ValidationResult.Failure(_errors)
            : ValidationResult.Success();
    }

    private async Task ValidatePulseExistsAndNotDeletedAsync(Guid pulseId, CancellationToken cancellationToken)
    {
        var pulse = await _unitOfWork.Pulses.FindByIdAsync(pulseId, cancellationToken);

        if (pulse is null)
        {
            throw new PulseNotFoundException(pulseId);
        }

        if (pulse.IsDeleted)
        {
            throw new PulseAlreadyDeletedException(pulseId);
        }
    }

    private async Task ValidateUserHasLikedPulseAsync(Guid pulseId, Guid profileId, CancellationToken cancellationToken)
    {
        var hasLiked = await _unitOfWork.Pulses.HasUserLikedPulseAsync(
            pulseId,
            profileId,
            cancellationToken);

        if (!hasLiked)
        {
            throw new PulseNotLikedException(pulseId, profileId);
        }
    }
}
