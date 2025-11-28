using System.Text;
using System.Text.Json;
using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse;

namespace Codewrinkles.Application.Pulse;

public sealed record GetPulsesByAuthorQuery(
    Guid AuthorId,
    Guid? CurrentUserId,
    string? Cursor,
    int Limit = 20
) : ICommand<FeedResponse>;

public sealed class GetPulsesByAuthorQueryHandler : ICommandHandler<GetPulsesByAuthorQuery, FeedResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPulsesByAuthorQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<FeedResponse> HandleAsync(
        GetPulsesByAuthorQuery query,
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

        // Fetch pulses from repository
        var pulses = await _unitOfWork.Pulses.GetByAuthorIdAsync(
            authorId: query.AuthorId,
            limit: query.Limit + 1, // Fetch one extra to determine if there are more
            beforeCreatedAt: beforeCreatedAt,
            beforeId: beforeId,
            cancellationToken: cancellationToken);

        // Determine if there are more results
        var hasMore = pulses.Count > query.Limit;
        var pulsesToReturn = hasMore ? pulses.Take(query.Limit).ToList() : pulses;

        // Generate next cursor
        string? nextCursor = null;
        if (hasMore)
        {
            var lastPulse = pulsesToReturn.Last();
            nextCursor = EncodeCursor(lastPulse.CreatedAt, lastPulse.Id);
        }

        // Get metadata for pulses in parallel to reduce latency
        HashSet<Guid> likedPulseIds = [];
        HashSet<Guid> bookmarkedPulseIds = [];
        HashSet<Guid> followingProfileIds = [];
        Dictionary<Guid, List<PulseMention>> mentionsByPulse;

        if (query.CurrentUserId.HasValue)
        {
            var pulseIds = pulsesToReturn.Select(p => p.Id);
            var authorIds = pulsesToReturn.Select(p => p.AuthorId).Distinct();
            var allPulseIds = pulsesToReturn.Select(p => p.Id).ToList();

            // Execute all metadata queries in parallel for better performance
            var likesTask = _unitOfWork.Pulses.GetLikedPulseIdsAsync(pulseIds, query.CurrentUserId.Value, cancellationToken);
            var bookmarksTask = _unitOfWork.Bookmarks.GetBookmarkedPulseIdsAsync(pulseIds, query.CurrentUserId.Value, cancellationToken);
            var followingTask = _unitOfWork.Follows.GetFollowingProfileIdsAsync(authorIds, query.CurrentUserId.Value, cancellationToken);
            var mentionsTask = _unitOfWork.Pulses.GetMentionsForPulsesAsync(allPulseIds, cancellationToken);

            await Task.WhenAll(likesTask, bookmarksTask, followingTask, mentionsTask);

            likedPulseIds = await likesTask;
            bookmarkedPulseIds = await bookmarksTask;
            followingProfileIds = await followingTask;
            var mentions = await mentionsTask;
            mentionsByPulse = mentions.GroupBy(m => m.PulseId).ToDictionary(g => g.Key, g => g.ToList());
        }
        else
        {
            // Load mentions for unauthenticated users
            var allPulseIds = pulsesToReturn.Select(p => p.Id).ToList();
            var mentions = await _unitOfWork.Pulses.GetMentionsForPulsesAsync(allPulseIds, cancellationToken);
            mentionsByPulse = mentions.GroupBy(m => m.PulseId).ToDictionary(g => g.Key, g => g.ToList());
        }

        // Map to DTOs
        var pulseDtos = pulsesToReturn.Select(p => MapToPulseDto(p, likedPulseIds, followingProfileIds, bookmarkedPulseIds, mentionsByPulse)).ToList();

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
