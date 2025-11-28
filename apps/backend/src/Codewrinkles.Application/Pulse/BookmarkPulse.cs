using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Pulse;

public sealed record BookmarkPulseCommand(
    Guid ProfileId,
    Guid PulseId
) : ICommand<BookmarkPulseResult>;

public sealed record BookmarkPulseResult(
    bool Success
);

public sealed class BookmarkPulseCommandHandler : ICommandHandler<BookmarkPulseCommand, BookmarkPulseResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public BookmarkPulseCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BookmarkPulseResult> HandleAsync(BookmarkPulseCommand command, CancellationToken cancellationToken)
    {
        // Validator has already confirmed pulse exists and is not already bookmarked
        var bookmark = PulseBookmark.Create(command.PulseId, command.ProfileId);

        _unitOfWork.Bookmarks.Create(bookmark);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new BookmarkPulseResult(Success: true);
    }
}
