using System.Text;
using System.Text.Json;
using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Social;

public sealed record GetFollowingQuery(
    Guid ProfileId,
    string? Cursor,
    int Limit = 20
) : ICommand<FollowingResponse>;

public sealed class GetFollowingQueryHandler : ICommandHandler<GetFollowingQuery, FollowingResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetFollowingQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<FollowingResponse> HandleAsync(
        GetFollowingQuery query,
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

        // Fetch following from repository
        var following = await _unitOfWork.Follows.GetFollowingAsync(
            profileId: query.ProfileId,
            limit: query.Limit + 1, // Fetch one extra to determine if there are more
            beforeCreatedAt: beforeCreatedAt,
            beforeId: beforeId,
            cancellationToken: cancellationToken);

        // Get total count
        var totalCount = await _unitOfWork.Follows.GetFollowingCountAsync(
            query.ProfileId,
            cancellationToken);

        // Determine if there are more results
        var hasMore = following.Count > query.Limit;
        var followingToReturn = hasMore ? following.Take(query.Limit).ToList() : following;

        // Generate next cursor
        string? nextCursor = null;
        if (hasMore)
        {
            var lastFollowing = followingToReturn.Last();
            nextCursor = EncodeCursor(DateTime.UtcNow, lastFollowing.Id);
        }

        // Map to DTOs
        var followingDtos = followingToReturn.Select(p => new FollowingDto(
            ProfileId: p.Id,
            Name: p.Name,
            Handle: p.Handle ?? string.Empty,
            AvatarUrl: p.AvatarUrl,
            Bio: p.Bio,
            FollowedAt: DateTime.UtcNow // TODO: This should come from Follow.CreatedAt
        )).ToList();

        return new FollowingResponse(
            Following: followingDtos,
            TotalCount: totalCount,
            NextCursor: nextCursor,
            HasMore: hasMore
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
