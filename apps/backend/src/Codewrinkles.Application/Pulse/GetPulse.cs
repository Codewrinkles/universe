using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse;
using Codewrinkles.Domain.Pulse.Exceptions;
using Codewrinkles.Telemetry;

namespace Codewrinkles.Application.Pulse;

public sealed record GetPulseQuery(
    Guid PulseId,
    Guid? CurrentUserId
) : ICommand<PulseDto>;

public sealed class GetPulseQueryHandler : ICommandHandler<GetPulseQuery, PulseDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPulseQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PulseDto> HandleAsync(
        GetPulseQuery query,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Pulse.Get);
        activity?.SetTag(TagNames.Pulse.Id, query.PulseId.ToString());
        if (query.CurrentUserId.HasValue)
        {
            activity?.SetProfileId(query.CurrentUserId.Value);
        }

        try
        {
            var pulse = await _unitOfWork.Pulses.FindByIdWithDetailsAsync(
                query.PulseId,
                cancellationToken);

            if (pulse is null)
            {
                throw new PulseNotFoundException(query.PulseId);
            }

            // Check if current user has liked this pulse, bookmarked it, and is following the author
            var isLikedByCurrentUser = false;
            var isBookmarkedByCurrentUser = false;
            var isFollowingAuthor = false;
            if (query.CurrentUserId.HasValue)
            {
                isLikedByCurrentUser = await _unitOfWork.Pulses.HasUserLikedPulseAsync(
                    query.PulseId,
                    query.CurrentUserId.Value,
                    cancellationToken);

                isBookmarkedByCurrentUser = await _unitOfWork.Bookmarks.IsBookmarkedAsync(
                    query.CurrentUserId.Value,
                    query.PulseId,
                    cancellationToken);

                isFollowingAuthor = await _unitOfWork.Follows.IsFollowingAsync(
                    query.CurrentUserId.Value,
                    pulse.AuthorId,
                    cancellationToken);
            }

            // Load mentions for the pulse
            var mentions = await _unitOfWork.Pulses.GetMentionsForPulsesAsync([pulse.Id], cancellationToken);

            activity?.SetTag(TagNames.Pulse.Type, pulse.Type.ToString().ToLowerInvariant());
            activity?.SetSuccess(true);

            return MapToPulseDto(pulse, isLikedByCurrentUser, isFollowingAuthor, isBookmarkedByCurrentUser, mentions.ToList());
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }

    private static PulseDto MapToPulseDto(
        Domain.Pulse.Pulse pulse,
        bool isLikedByCurrentUser,
        bool isFollowingAuthor,
        bool isBookmarkedByCurrentUser,
        List<PulseMention> mentions)
    {
        var mentionDtos = mentions
            .Select(m => new MentionDto(m.ProfileId, m.Handle))
            .ToList();

        return new PulseDto(
            Id: pulse.Id,
            Author: new PulseAuthorDto(
                Id: pulse.Author.Id,
                Name: pulse.Author.Name,
                Handle: pulse.Author.Handle ?? string.Empty,
                AvatarUrl: pulse.Author.AvatarUrl
            ),
            Content: pulse.Content,
            Type: pulse.Type.ToString().ToLowerInvariant(),
            CreatedAt: pulse.CreatedAt,
            Engagement: new PulseEngagementDto(
                ReplyCount: pulse.Engagement.ReplyCount,
                RepulseCount: pulse.Engagement.RepulseCount,
                LikeCount: pulse.Engagement.LikeCount,
                ViewCount: pulse.Engagement.ViewCount
            ),
            IsLikedByCurrentUser: isLikedByCurrentUser,
            IsFollowingAuthor: isFollowingAuthor,
            IsBookmarkedByCurrentUser: isBookmarkedByCurrentUser,
            ParentPulseId: pulse.ParentPulseId,
            ThreadRootId: pulse.ThreadRootId,
            ReplyingTo: null, // Not loaded in single pulse context
            RepulsedPulse: pulse.RepulsedPulse is not null
                ? MapToRepulsedPulseDto(pulse.RepulsedPulse)
                : null,
            ImageUrl: pulse.Image?.Url,
            LinkPreview: pulse.LinkPreview is not null
                ? new PulseLinkPreviewDto(
                    Url: pulse.LinkPreview.Url,
                    Title: pulse.LinkPreview.Title,
                    Description: pulse.LinkPreview.Description,
                    ImageUrl: pulse.LinkPreview.ImageUrl,
                    Domain: pulse.LinkPreview.Domain)
                : null,
            Mentions: mentionDtos
        );
    }

    private static RepulsedPulseDto MapToRepulsedPulseDto(Domain.Pulse.Pulse pulse)
    {
        return new RepulsedPulseDto(
            Id: pulse.Id,
            Author: new PulseAuthorDto(
                Id: pulse.Author.Id,
                Name: pulse.Author.Name,
                Handle: pulse.Author.Handle ?? string.Empty,
                AvatarUrl: pulse.Author.AvatarUrl
            ),
            Content: pulse.Content,
            CreatedAt: pulse.CreatedAt,
            IsDeleted: pulse.IsDeleted
        );
    }
}
