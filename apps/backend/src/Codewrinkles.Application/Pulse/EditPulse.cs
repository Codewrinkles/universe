using System.Data;
using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Telemetry;

namespace Codewrinkles.Application.Pulse;

public sealed record EditPulseCommand(
    Guid PulseId,
    Guid ProfileId,
    string NewContent
) : ICommand<EditPulseResult>;

public sealed record EditPulseResult(
    bool Success,
    string Content,
    DateTimeOffset UpdatedAt
);

public sealed class EditPulseCommandHandler
    : ICommandHandler<EditPulseCommand, EditPulseResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PulseContentProcessor _contentProcessor;

    public EditPulseCommandHandler(
        IUnitOfWork unitOfWork,
        PulseContentProcessor contentProcessor)
    {
        _unitOfWork = unitOfWork;
        _contentProcessor = contentProcessor;
    }

    public async Task<EditPulseResult> HandleAsync(
        EditPulseCommand command,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Pulse.Edit);
        activity?.SetProfileId(command.ProfileId);
        activity?.SetTag(TagNames.Pulse.Id, command.PulseId.ToString());

        try
        {
            // Validator has already confirmed:
            // - Pulse exists
            // - Pulse is not deleted
            // - User is the author
            // - New content is valid (not empty, within length limit)

            // Perform all operations atomically within a transaction
            await using var transaction = await _unitOfWork.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

            try
            {
                // 1. Update pulse content
                var pulse = await _unitOfWork.Pulses.FindByIdAsync(command.PulseId, cancellationToken);
                pulse!.UpdateContent(command.NewContent);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 2. Clear and re-process mentions
                // Delete existing mentions
                await _unitOfWork.Pulses.DeleteMentionsForPulseAsync(command.PulseId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Create new mentions based on updated content
                await _contentProcessor.ProcessMentionsAsync(
                    command.PulseId,
                    pulse.Content,
                    cancellationToken);

                // 3. Clear and re-process hashtags
                // Get existing hashtags and decrement their usage counts
                var existingHashtags = await _unitOfWork.Hashtags.GetHashtagsForPulseAsync(
                    command.PulseId,
                    cancellationToken);

                foreach (var hashtag in existingHashtags)
                {
                    hashtag.DecrementUsage();
                }

                // Delete pulse-hashtag associations
                await _unitOfWork.Hashtags.DeletePulseHashtagsForPulseAsync(
                    command.PulseId,
                    cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Create new hashtag associations based on updated content
                await _contentProcessor.ProcessHashtagsAsync(
                    command.PulseId,
                    pulse.Content,
                    cancellationToken);

                // Commit transaction
                await transaction.CommitAsync(cancellationToken);

                AppMetrics.RecordPulseEdited();
                activity?.SetSuccess(true);

                return new EditPulseResult(
                    Success: true,
                    Content: pulse.Content,
                    UpdatedAt: pulse.UpdatedAt!.Value
                );
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}
