using System.Text;
using System.Text.Json;
using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Social;

public sealed record GetFollowersQuery(
    Guid ProfileId,
    Guid? CurrentUserId,
    string? Cursor,
    int Limit = 20
) : ICommand<FollowersResponse>;

public sealed class GetFollowersQueryHandler : ICommandHandler<GetFollowersQuery, FollowersResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetFollowersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<FollowersResponse> HandleAsync(
        GetFollowersQuery query,
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

        // Fetch followers from repository
        var followers = await _unitOfWork.Follows.GetFollowersAsync(
            profileId: query.ProfileId,
            limit: query.Limit + 1, // Fetch one extra to determine if there are more
            beforeCreatedAt: beforeCreatedAt,
            beforeId: beforeId,
            cancellationToken: cancellationToken);

        // Get total count
        var totalCount = await _unitOfWork.Follows.GetFollowerCountAsync(
            query.ProfileId,
            cancellationToken);

        // Determine if there are more results
        var hasMore = followers.Count > query.Limit;
        var followersToReturn = hasMore ? followers.Take(query.Limit).ToList() : followers;

        // Generate next cursor
        string? nextCursor = null;
        if (hasMore)
        {
            var lastFollower = followersToReturn.Last();
            // Note: We need to get the follow CreatedAt, which we'll approximate with DateTime.UtcNow for now
            // This is a limitation - ideally we'd return Follow entities with Profile data
            nextCursor = EncodeCursor(DateTime.UtcNow, lastFollower.Id);
        }

        // Batch check which followers the current user is following (if authenticated)
        var followingProfileIds = new HashSet<Guid>();
        if (query.CurrentUserId.HasValue)
        {
            var followerIds = followersToReturn.Select(p => p.Id).ToList();
            followingProfileIds = await _unitOfWork.Follows.GetFollowingProfileIdsAsync(
                followerIds,
                query.CurrentUserId.Value,
                cancellationToken);
        }

        // Map to DTOs
        var followerDtos = followersToReturn.Select(p => new FollowerDto(
            ProfileId: p.Id,
            Name: p.Name,
            Handle: p.Handle ?? string.Empty,
            AvatarUrl: p.AvatarUrl,
            Bio: p.Bio,
            FollowedAt: DateTime.UtcNow, // TODO: This should come from Follow.CreatedAt
            IsFollowing: followingProfileIds.Contains(p.Id)
        )).ToList();

        return new FollowersResponse(
            Followers: followerDtos,
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
