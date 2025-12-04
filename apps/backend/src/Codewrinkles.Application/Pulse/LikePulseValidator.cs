using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse.Exceptions;
using Kommand;

namespace Codewrinkles.Application.Pulse;

public sealed class LikePulseValidator : IValidator<LikePulseCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private List<ValidationError> _errors = null!;

    public LikePulseValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ValidationResult> ValidateAsync(
        LikePulseCommand command,
        CancellationToken cancellationToken)
    {
        _errors = [];

        // Business rule validation
        await ValidatePulseExistsAndNotDeletedAsync(command.PulseId, cancellationToken);
        await ValidateNotAlreadyLikedAsync(command.PulseId, command.ProfileId, cancellationToken);

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

    private async Task ValidateNotAlreadyLikedAsync(Guid pulseId, Guid profileId, CancellationToken cancellationToken)
    {
        var alreadyLiked = await _unitOfWork.Pulses.HasUserLikedPulseAsync(
            pulseId,
            profileId,
            cancellationToken);

        if (alreadyLiked)
        {
            throw new PulseAlreadyLikedException(pulseId, profileId);
        }
    }
}
