using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse;

namespace Codewrinkles.Application.Pulse;

public sealed record CreatePulseCommand(
    Guid AuthorId,
    string Content
) : ICommand<CreatePulseResult>;

public sealed record CreatePulseResult(
    Guid PulseId,
    string Content,
    DateTime CreatedAt
);

public sealed class CreatePulseCommandHandler
    : ICommandHandler<CreatePulseCommand, CreatePulseResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreatePulseCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CreatePulseResult> HandleAsync(
        CreatePulseCommand command,
        CancellationToken cancellationToken)
    {
        // Create Pulse entity
        var pulse = Domain.Pulse.Pulse.Create(
            authorId: command.AuthorId,
            content: command.Content);

        _unitOfWork.Pulses.Create(pulse);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Create PulseEngagement with zero counts
        var engagement = PulseEngagement.Create(pulse.Id);
        _unitOfWork.Pulses.CreateEngagement(engagement);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreatePulseResult(
            PulseId: pulse.Id,
            Content: pulse.Content,
            CreatedAt: pulse.CreatedAt
        );
    }
}
