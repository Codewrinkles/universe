using System.Data;
using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Telemetry;

namespace Codewrinkles.Application.Pulse;

public sealed record DeletePulseCommand(
    Guid ProfileId,
    Guid PulseId
) : ICommand<DeletePulseResult>;

public sealed record DeletePulseResult(
    bool Success
);

public sealed class DeletePulseCommandHandler
    : ICommandHandler<DeletePulseCommand, DeletePulseResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeletePulseCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DeletePulseResult> HandleAsync(
        DeletePulseCommand command,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Pulse.Delete);
        activity?.SetProfileId(command.ProfileId);
        activity?.SetTag(TagNames.Pulse.Id, command.PulseId.ToString());

        try
        {
            // Validator has already confirmed:
            // - Pulse exists
            // - User is the author
            // So we can safely fetch and delete

            var pulse = await _unitOfWork.Pulses.FindByIdAsync(command.PulseId, cancellationToken);

            // Pulse is guaranteed to exist (validator checked)
            pulse!.MarkAsDeleted();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            AppMetrics.RecordPulseDeleted();
            activity?.SetSuccess(true);

            return new DeletePulseResult(Success: true);
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}
