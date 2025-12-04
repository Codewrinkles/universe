using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse;
using Codewrinkles.Telemetry;
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
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Engagement.Bookmark);
        activity?.SetProfileId(command.ProfileId);
        activity?.SetTag(TagNames.Pulse.Id, command.PulseId.ToString());

        try
        {
            // Validator has already confirmed pulse exists and is not already bookmarked
            var bookmark = PulseBookmark.Create(command.PulseId, command.ProfileId);

            _unitOfWork.Bookmarks.Create(bookmark);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            AppMetrics.RecordPulseBookmark();
            activity?.SetSuccess(true);

            return new BookmarkPulseResult(Success: true);
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}
