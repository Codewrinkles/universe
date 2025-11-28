using System.Text;
using System.Text.Json;
using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse;

namespace Codewrinkles.Application.Pulse;

public sealed record GetBookmarkedPulsesQuery(
    Guid ProfileId,
    string? Cursor,
    int Limit = 20
) : ICommand<FeedResponse>;

public sealed class GetBookmarkedPulsesQueryHandler : ICommandHandler<GetBookmarkedPulsesQuery, FeedResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetBookmarkedPulsesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<FeedResponse> HandleAsync(
        GetBookmarkedPulsesQuery query,
        CancellationToken cancellationToken)
    {
        // Decode cursor if provided
        DateTime? beforeCreatedAt = null;
        Guid? beforeId = null;

        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            var cursor = DecodeCursor(query.Cursor);
            beforeCreatedAt = cursor.CreatedAt;
            beforeId = cursor.Id;
        }

        // Fetch bookmarked pulses from repository
        var (pulses, hasMore) = await _unitOfWork.Bookmarks.GetBookmarkedPulsesAsync(
            profileId: query.ProfileId,
            limit: query.Limit,
            beforeCreatedAt: beforeCreatedAt,
            beforeId: beforeId,
            cancellationToken: cancellationToken);

        // Generate next cursor
        string? nextCursor = null;
        if (hasMore && pulses.Count > 0)
        {
            var lastPulse = pulses.Last();
            nextCursor = EncodeCursor(lastPulse.CreatedAt, lastPulse.Id);
        }

        // Get liked pulse IDs and followed profile IDs for current user
        HashSet<Guid> likedPulseIds = [];
        HashSet<Guid> followingProfileIds = [];

        if (pulses.Count > 0)
        {
            var pulseIds = pulses.Select(p => p.Id);
            likedPulseIds = await _unitOfWork.Pulses.GetLikedPulseIdsAsync(
                pulseIds,
                query.ProfileId,
                cancellationToken);

            var authorIds = pulses.Select(p => p.AuthorId).Distinct();
            followingProfileIds = await _unitOfWork.Follows.GetFollowingProfileIdsAsync(
                authorIds,
                query.ProfileId,
                cancellationToken);
        }

        // Load mentions for all pulses (batch load to avoid N+1 queries)
        var allPulseIds = pulses.Select(p => p.Id).ToList();
        var mentions = await _unitOfWork.Pulses.GetMentionsForPulsesAsync(allPulseIds, cancellationToken);
        var mentionsByPulse = mentions.GroupBy(m => m.PulseId).ToDictionary(g => g.Key, g => g.ToList());

        // Map to DTOs (all pulses are bookmarked by definition)
        var bookmarkedPulseIds = pulses.Select(p => p.Id).ToHashSet();
        var pulseDtos = pulses.Select(p => MapToPulseDto(p, likedPulseIds, followingProfileIds, bookmarkedPulseIds, mentionsByPulse)).ToList();

        return new FeedResponse(
            Pulses: pulseDtos,
            NextCursor: nextCursor,
            HasMore: hasMore
        );
    }

    private static PulseDto MapToPulseDto(
        Domain.Pulse.Pulse pulse,
        HashSet<Guid> likedPulseIds,
        HashSet<Guid> followingProfileIds,
        HashSet<Guid> bookmarkedPulseIds,
        Dictionary<Guid, List<PulseMention>> mentionsByPulse)
    {
        var mentions = mentionsByPulse.TryGetValue(pulse.Id, out var pulseMentions)
            ? pulseMentions.Select(m => new MentionDto(m.ProfileId, m.Handle)).ToList()
            : [];

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
            IsLikedByCurrentUser: likedPulseIds.Contains(pulse.Id),
            IsFollowingAuthor: followingProfileIds.Contains(pulse.AuthorId),
            IsBookmarkedByCurrentUser: bookmarkedPulseIds.Contains(pulse.Id),
            ParentPulseId: pulse.ParentPulseId,
            RepulsedPulse: pulse.RepulsedPulse is not null
                ? MapToRepulsedPulseDto(pulse.RepulsedPulse)
                : null,
            ImageUrl: pulse.Image?.Url,
            Mentions: mentions
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

    private static string EncodeCursor(DateTime createdAt, Guid id)
    {
        var cursor = new { CreatedAt = createdAt, Id = id };
        var json = JsonSerializer.Serialize(cursor);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes);
    }

    private static (DateTime CreatedAt, Guid Id) DecodeCursor(string cursor)
    {
        try
        {
            var bytes = Convert.FromBase64String(cursor);
            var json = Encoding.UTF8.GetString(bytes);
            var cursorData = JsonSerializer.Deserialize<CursorData>(json);

            if (cursorData is null)
            {
                throw new InvalidOperationException("Invalid cursor format");
            }

            return (cursorData.CreatedAt, cursorData.Id);
        }
        catch
        {
            throw new InvalidOperationException("Invalid cursor format");
        }
    }

    private sealed record CursorData(DateTime CreatedAt, Guid Id);
}
