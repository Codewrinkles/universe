using System.Data;
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

        // Perform all operations atomically within a transaction
        // Isolation Level: ReadCommitted
        // - Prevents dirty reads (other transactions won't see uncommitted unlike)
        // - Prevents partial state (like record deleted without engagement count update)
        // TODO: Lost update problem - concurrent unlikes may result in incorrect count
        //       Solution: Add optimistic concurrency (RowVersion) or use SQL-level atomic decrement
        await using var transaction = await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        try
        {
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

            // Commit transaction - like deleted and count decremented successfully
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            // Rollback on any error - no database changes persisted
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return new UnlikePulseResult(Success: true);
    }
}
