using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse;
using Codewrinkles.Domain.Pulse.Exceptions;

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
        // 1. Get parent pulse with details
        var parentPulse = await _unitOfWork.Pulses.FindByIdWithDetailsAsync(
            query.PulseId,
            cancellationToken);

        if (parentPulse is null)
        {
            throw new PulseNotFoundException(query.PulseId);
        }

        // 2. Get replies with pagination
        var replies = await _unitOfWork.Pulses.GetRepliesByParentIdAsync(
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

        // 4. Get total reply count
        var totalReplyCount = await _unitOfWork.Pulses.GetReplyCountAsync(
            query.PulseId,
            cancellationToken);

        // 5. Get liked pulse IDs and followed profile IDs for current user (parent + all replies)
        var allPulseIds = new List<Guid> { parentPulse.Id };
        allPulseIds.AddRange(repliesToReturn.Select(r => r.Id));

        HashSet<Guid> likedPulseIds = [];
        HashSet<Guid> followingProfileIds = [];
        if (query.CurrentUserId.HasValue)
        {
            likedPulseIds = await _unitOfWork.Pulses.GetLikedPulseIdsAsync(
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
            mentionsByPulse.TryGetValue(parentPulse.Id, out var parentMentions) ? parentMentions : []);
        var replyDtos = repliesToReturn
            .Select(r => MapToPulseDto(
                r,
                likedPulseIds.Contains(r.Id),
                followingProfileIds.Contains(r.AuthorId),
                mentionsByPulse.TryGetValue(r.Id, out var replyMentions) ? replyMentions : []))
            .ToList();

        return new ThreadResponse(
            ParentPulse: parentDto,
            Replies: replyDtos,
            TotalReplyCount: totalReplyCount,
            NextCursor: nextCursor,
            HasMore: hasMore
        );
    }

    private static PulseDto MapToPulseDto(
        Domain.Pulse.Pulse pulse,
        bool isLikedByCurrentUser,
        bool isFollowingAuthor,
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
            ParentPulseId: pulse.ParentPulseId,
            RepulsedPulse: pulse.RepulsedPulse is not null
                ? MapToRepulsedPulseDto(pulse.RepulsedPulse)
                : null,
            ImageUrl: pulse.Image?.Url,
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
