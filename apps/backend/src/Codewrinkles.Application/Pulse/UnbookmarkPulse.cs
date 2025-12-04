using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Pulse;

public sealed record UnbookmarkPulseCommand(
    Guid ProfileId,
    Guid PulseId
) : ICommand<UnbookmarkPulseResult>;

public sealed record UnbookmarkPulseResult(
    bool Success
);

public sealed class UnbookmarkPulseCommandHandler : ICommandHandler<UnbookmarkPulseCommand, UnbookmarkPulseResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public UnbookmarkPulseCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UnbookmarkPulseResult> HandleAsync(UnbookmarkPulseCommand command, CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Engagement.Unbookmark);
        activity?.SetProfileId(command.ProfileId);
        activity?.SetTag(TagNames.Pulse.Id, command.PulseId.ToString());

        try
        {
            // Validator has already confirmed bookmark exists
            var bookmark = await _unitOfWork.Bookmarks.FindByProfileAndPulseAsync(
                command.ProfileId,
                command.PulseId,
                cancellationToken);

            if (bookmark is not null)
            {
                _unitOfWork.Bookmarks.Delete(bookmark);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            activity?.SetSuccess(true);

            return new UnbookmarkPulseResult(Success: true);
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}
