using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Pulse;

public sealed record UnlikePulseCommand(
    Guid PulseId,
    Guid ProfileId
) : ICommand<UnlikePulseResult>;

public sealed record UnlikePulseResult(
    bool Success
);

public sealed class UnlikePulseCommandHandler
    : ICommandHandler<UnlikePulseCommand, UnlikePulseResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public UnlikePulseCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UnlikePulseResult> HandleAsync(
        UnlikePulseCommand command,
        CancellationToken cancellationToken)
    {
        // Validator has already confirmed:
        // - Pulse exists and is not deleted
        // - User has liked this pulse

        // Find and delete the PulseLike entity
        var pulseLike = await _unitOfWork.Pulses.FindLikeAsync(
            command.PulseId,
            command.ProfileId,
            cancellationToken);

        // PulseLike is guaranteed to exist after validation
        _unitOfWork.Pulses.DeleteLike(pulseLike!);

        // Get engagement with tracking to update like count
        var engagement = await _unitOfWork.Pulses.FindEngagementAsync(command.PulseId, cancellationToken);
        // Engagement is guaranteed to exist (created with pulse)
        engagement!.DecrementLikeCount();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UnlikePulseResult(Success: true);
    }
}
