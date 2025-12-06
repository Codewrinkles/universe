using System.Text;
using System.Text.Json;
using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse;
using Codewrinkles.Telemetry;

namespace Codewrinkles.Application.Pulse;

public sealed record GetFeedQuery(
    Guid? CurrentUserId,
    string? Cursor,
    int Limit = 20
) : ICommand<FeedResponse>;

public sealed class GetFeedQueryHandler : ICommandHandler<GetFeedQuery, FeedResponse>
{
    private readonly IPulseRepository _pulseRepository;

    public GetFeedQueryHandler(IPulseRepository pulseRepository)
    {
        _pulseRepository = pulseRepository;
    }

    public async Task<FeedResponse> HandleAsync(
        GetFeedQuery query,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Feed.Load);

        var isAuthenticated = query.CurrentUserId.HasValue;
        if (query.CurrentUserId.HasValue)
        {
            activity?.SetProfileId(query.CurrentUserId.Value);
        }

        try
        {
            // Decode cursor if provided
            DateTime? beforeCreatedAt = null;
            Guid? beforeId = null;
            var cursorType = "initial";

            if (!string.IsNullOrWhiteSpace(query.Cursor))
            {
                var cursor = DecodeCursor(query.Cursor);
                beforeCreatedAt = cursor.CreatedAt;
                beforeId = cursor.Id;
                cursorType = "pagination";
            }

            // Fetch pulses and all metadata in single repository call
            // Repository handles parallel query optimization internally
            var feedData = await _pulseRepository.GetFeedWithMetadataAsync(
                currentUserId: query.CurrentUserId,
                limit: query.Limit + 1, // Fetch one extra to determine if there are more
                beforeCreatedAt: beforeCreatedAt,
                beforeId: beforeId,
                cancellationToken: cancellationToken);

            // Determine if there are more results
            var hasMore = feedData.Pulses.Count > query.Limit;
            var pulsesToReturn = hasMore ? feedData.Pulses.Take(query.Limit).ToList() : feedData.Pulses;

            // Generate next cursor
            string? nextCursor = null;
            if (hasMore)
            {
                var lastPulse = pulsesToReturn.Last();
                nextCursor = EncodeCursor(lastPulse.CreatedAt, lastPulse.Id);
            }

            // Group mentions by pulse for DTO mapping
            var mentionsByPulse = feedData.Mentions
                .GroupBy(m => m.PulseId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Map to DTOs
            var pulseDtos = pulsesToReturn.Select(p => MapToPulseDto(
                p,
                feedData.LikedPulseIds,
                feedData.FollowingProfileIds,
                feedData.BookmarkedPulseIds,
                mentionsByPulse)).ToList();

            // Record metrics and set span metadata
            AppMetrics.RecordFeedLoad(authenticated: isAuthenticated);
            activity?.SetFeedMetadata(
                resultCount: pulseDtos.Count,
                hasMore: hasMore,
                cursorType: cursorType);
            activity?.SetSuccess(true);

            return new FeedResponse(
                Pulses: pulseDtos,
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
        HashSet<Guid> likedPulseIds,
        HashSet<Guid> followingProfileIds,
        HashSet<Guid> bookmarkedPulseIds,
        Dictionary<Guid, List<PulseMention>> mentionsByPulse)
    {
        var mentions = mentionsByPulse.TryGetValue(pulse.Id, out var pulseMentions)
            ? pulseMentions.Select(m => new MentionDto(m.ProfileId, m.Handle)).ToList()
            : [];

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
            IsLikedByCurrentUser: likedPulseIds.Contains(pulse.Id),
            IsFollowingAuthor: followingProfileIds.Contains(pulse.AuthorId),
            IsBookmarkedByCurrentUser: bookmarkedPulseIds.Contains(pulse.Id),
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
