using System.Text;
using System.Text.Json;
using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Pulse;

public sealed record GetPulsesByHashtagQuery(
    string Tag,
    Guid? CurrentUserId,
    string? Cursor,
    int Limit = 20
) : ICommand<FeedResponse>;

public sealed class GetPulsesByHashtagQueryHandler
    : ICommandHandler<GetPulsesByHashtagQuery, FeedResponse>
{
    private readonly IHashtagRepository _hashtagRepository;

    public GetPulsesByHashtagQueryHandler(IHashtagRepository hashtagRepository)
    {
        _hashtagRepository = hashtagRepository;
    }

    public async Task<FeedResponse> HandleAsync(
        GetPulsesByHashtagQuery query,
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

        // Fetch pulses by hashtag
        var pulses = await _hashtagRepository.GetPulsesByHashtagAsync(
            tag: query.Tag,
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

        // Map to DTOs (simplified - no metadata for now)
        var pulseDtos = pulsesToReturn.Select(p => MapToPulseDto(p)).ToList();

        return new FeedResponse(
            Pulses: pulseDtos,
            NextCursor: nextCursor,
            HasMore: hasMore
        );
    }

    private static PulseDto MapToPulseDto(Domain.Pulse.Pulse pulse)
    {
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
            IsLikedByCurrentUser: false, // Simplified - no metadata
            IsFollowingAuthor: false, // Simplified - no metadata
            IsBookmarkedByCurrentUser: false, // Simplified - no metadata
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
            Mentions: [] // Simplified - no mentions for now
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
