using Codewrinkles.Application.Common.Interfaces;
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

        return new UnbookmarkPulseResult(Success: true);
    }
}
