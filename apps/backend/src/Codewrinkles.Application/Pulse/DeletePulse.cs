using System.Data;
using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

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
        // Validator has already confirmed:
        // - Pulse exists
        // - User is the author
        // So we can safely fetch and delete

        var pulse = await _unitOfWork.Pulses.FindByIdAsync(command.PulseId, cancellationToken);

        // Pulse is guaranteed to exist (validator checked)
        pulse!.MarkAsDeleted();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeletePulseResult(Success: true);
    }
}
