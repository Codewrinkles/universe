using System.Text;
using System.Text.Json;
using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse;

namespace Codewrinkles.Application.Pulse;

public sealed record GetFeedQuery(
    Guid? CurrentUserId,
    string? Cursor,
    int Limit = 20
) : ICommand<FeedResponse>;

public sealed class GetFeedQueryHandler : ICommandHandler<GetFeedQuery, FeedResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetFeedQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<FeedResponse> HandleAsync(
        GetFeedQuery query,
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
        var pulses = await _unitOfWork.Pulses.GetFeedAsync(
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

        // Get liked pulse IDs for current user (if authenticated)
        HashSet<Guid> likedPulseIds = [];
        if (query.CurrentUserId.HasValue)
        {
            var pulseIds = pulsesToReturn.Select(p => p.Id);
            likedPulseIds = await _unitOfWork.Pulses.GetLikedPulseIdsAsync(
                pulseIds,
                query.CurrentUserId.Value,
                cancellationToken);
        }

        // Map to DTOs
        var pulseDtos = pulsesToReturn.Select(p => MapToPulseDto(p, likedPulseIds)).ToList();

        return new FeedResponse(
            Pulses: pulseDtos,
            NextCursor: nextCursor,
            HasMore: hasMore
        );
    }

    private static PulseDto MapToPulseDto(Domain.Pulse.Pulse pulse, HashSet<Guid> likedPulseIds)
    {
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
            RepulsedPulse: pulse.RepulsedPulse is not null
                ? MapToRepulsedPulseDto(pulse.RepulsedPulse)
                : null,
            ImageUrl: pulse.Image?.Url
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
