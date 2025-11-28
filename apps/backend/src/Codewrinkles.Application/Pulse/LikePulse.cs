using System.Data;
using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse;

namespace Codewrinkles.Application.Pulse;

public sealed record LikePulseCommand(
    Guid PulseId,
    Guid ProfileId
) : ICommand<LikePulseResult>;

public sealed record LikePulseResult(
    bool Success
);

public sealed class LikePulseCommandHandler
    : ICommandHandler<LikePulseCommand, LikePulseResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public LikePulseCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<LikePulseResult> HandleAsync(
        LikePulseCommand command,
        CancellationToken cancellationToken)
    {
        // Validator has already confirmed:
        // - Pulse exists and is not deleted
        // - User hasn't already liked this pulse

        // Perform all operations atomically within a transaction
        // Isolation Level: ReadCommitted
        // - Prevents dirty reads (other transactions won't see uncommitted like)
        // - Prevents partial state (like record without engagement count update)
        // TODO: Lost update problem - concurrent likes may result in incorrect count
        //       Solution: Add optimistic concurrency (RowVersion) or use SQL-level atomic increment
        await using var transaction = await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        try
        {
            // Create PulseLike entity
            var pulseLike = PulseLike.Create(
                pulseId: command.PulseId,
                profileId: command.ProfileId);

            _unitOfWork.Pulses.CreateLike(pulseLike);

            // Get engagement with tracking to update like count
            var engagement = await _unitOfWork.Pulses.FindEngagementAsync(command.PulseId, cancellationToken);
            // Engagement is guaranteed to exist (created with pulse)
            engagement!.IncrementLikeCount();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Commit transaction - like created and count incremented successfully
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            // Rollback on any error - no database changes persisted
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return new LikePulseResult(Success: true);
    }
}
