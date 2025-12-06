using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse;
using Codewrinkles.Domain.Pulse.Exceptions;
using Codewrinkles.Telemetry;

namespace Codewrinkles.Application.Pulse;

public sealed record GetThreadQuery(
    Guid PulseId,
    Guid? CurrentUserId,
    int Limit = 20,
    DateTime? BeforeCreatedAt = null,
    Guid? BeforeId = null
) : ICommand<ThreadResponse>;

public sealed class GetThreadQueryHandler : ICommandHandler<GetThreadQuery, ThreadResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetThreadQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ThreadResponse> HandleAsync(
        GetThreadQuery query,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Feed.Thread);
        activity?.SetTag(TagNames.Pulse.Id, query.PulseId.ToString());
        if (query.CurrentUserId.HasValue)
        {
            activity?.SetProfileId(query.CurrentUserId.Value);
        }

        try
        {
            // 1. Get parent pulse with details
            var parentPulse = await _unitOfWork.Pulses.FindByIdWithDetailsAsync(
                query.PulseId,
                cancellationToken);

            if (parentPulse is null)
            {
                throw new PulseNotFoundException(query.PulseId);
            }

            // 2. Get all replies in thread with pagination (flat display)
            var replies = await _unitOfWork.Pulses.GetRepliesByThreadRootIdAsync(
                query.PulseId,
                query.Limit + 1, // Fetch one extra to check if there are more
                query.BeforeCreatedAt,
                query.BeforeId,
                cancellationToken);

            // 3. Calculate pagination info
            var hasMore = replies.Count > query.Limit;
            var repliesToReturn = hasMore ? replies.Take(query.Limit).ToList() : replies;

            string? nextCursor = null;
            if (hasMore && repliesToReturn.Count > 0)
            {
                var lastReply = repliesToReturn[^1];
                nextCursor = $"{lastReply.CreatedAt:O}_{lastReply.Id}";
            }

            // 4. Get total reply count for thread
            var totalReplyCount = await _unitOfWork.Pulses.GetReplyCountByThreadRootIdAsync(
                query.PulseId,
                cancellationToken);

            // 5. Get liked pulse IDs, bookmarked pulse IDs, and followed profile IDs for current user (parent + all replies)
            var allPulseIds = new List<Guid> { parentPulse.Id };
            allPulseIds.AddRange(repliesToReturn.Select(r => r.Id));

            HashSet<Guid> likedPulseIds = [];
            HashSet<Guid> bookmarkedPulseIds = [];
            HashSet<Guid> followingProfileIds = [];
            if (query.CurrentUserId.HasValue)
            {
                likedPulseIds = await _unitOfWork.Pulses.GetLikedPulseIdsAsync(
                    allPulseIds,
                    query.CurrentUserId.Value,
                    cancellationToken);

                bookmarkedPulseIds = await _unitOfWork.Bookmarks.GetBookmarkedPulseIdsAsync(
                    allPulseIds,
                    query.CurrentUserId.Value,
                    cancellationToken);

                var allAuthorIds = new List<Guid> { parentPulse.AuthorId };
                allAuthorIds.AddRange(repliesToReturn.Select(r => r.AuthorId));
                var uniqueAuthorIds = allAuthorIds.Distinct();

                followingProfileIds = await _unitOfWork.Follows.GetFollowingProfileIdsAsync(
                    uniqueAuthorIds,
                    query.CurrentUserId.Value,
                    cancellationToken);
            }

            // Load mentions for all pulses (parent + replies)
            var mentions = await _unitOfWork.Pulses.GetMentionsForPulsesAsync(allPulseIds, cancellationToken);
            var mentionsByPulse = mentions.GroupBy(m => m.PulseId).ToDictionary(g => g.Key, g => g.ToList());

            // 6. Map to DTOs
            var parentDto = MapToPulseDto(
                parentPulse,
                likedPulseIds.Contains(parentPulse.Id),
                followingProfileIds.Contains(parentPulse.AuthorId),
                bookmarkedPulseIds.Contains(parentPulse.Id),
                mentionsByPulse.TryGetValue(parentPulse.Id, out var parentMentions) ? parentMentions : []);
            var replyDtos = repliesToReturn
                .Select(r => MapToPulseDto(
                    r,
                    likedPulseIds.Contains(r.Id),
                    followingProfileIds.Contains(r.AuthorId),
                    bookmarkedPulseIds.Contains(r.Id),
                    mentionsByPulse.TryGetValue(r.Id, out var replyMentions) ? replyMentions : []))
                .ToList();

            activity?.SetFeedMetadata(
                resultCount: replyDtos.Count,
                hasMore: hasMore);
            activity?.SetTag(TagNames.Database.RecordCount, totalReplyCount);
            activity?.SetSuccess(true);

            return new ThreadResponse(
                ParentPulse: parentDto,
                Replies: replyDtos,
                TotalReplyCount: totalReplyCount,
                NextCursor: nextCursor,
                HasMore: hasMore
            );
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

        // Populate ReplyingTo for ALL replies (not just nested ones)
        ReplyingToDto? replyingTo = null;
        if (pulse.ParentPulseId.HasValue && pulse.ParentPulse is not null)
        {
            replyingTo = new ReplyingToDto(
                PulseId: pulse.ParentPulse.Id,
                AuthorHandle: pulse.ParentPulse.Author?.Handle ?? "[deleted]",
                AuthorName: pulse.ParentPulse.Author?.Name ?? "[deleted]"
            );
        }

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
            ReplyingTo: replyingTo,
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
