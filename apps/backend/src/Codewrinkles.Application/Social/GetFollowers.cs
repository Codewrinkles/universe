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
        DateTimeOffset? beforeCreatedAt = null;
        Guid? beforeId = null;

        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            var cursor = DecodeCursor(query.Cursor);
            beforeCreatedAt = cursor.CreatedAt;
            beforeId = cursor.Id;
        }

        // Fetch followers from repository (now returns ProfileWithFollowDate)
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

        // Generate next cursor using the actual FollowedAt from the Follow entity
        string? nextCursor = null;
        if (hasMore)
        {
            var lastFollower = followersToReturn.Last();
            nextCursor = EncodeCursor(lastFollower.FollowedAt, lastFollower.Profile.Id);
        }

        // Batch check which followers the current user is following (if authenticated)
        var followingProfileIds = new HashSet<Guid>();
        if (query.CurrentUserId.HasValue)
        {
            var followerIds = followersToReturn.Select(f => f.Profile.Id).ToList();
            followingProfileIds = await _unitOfWork.Follows.GetFollowingProfileIdsAsync(
                followerIds,
                query.CurrentUserId.Value,
                cancellationToken);
        }

        // Map to DTOs - now using the actual FollowedAt from the Follow entity
        var followerDtos = followersToReturn.Select(f => new FollowerDto(
            ProfileId: f.Profile.Id,
            Name: f.Profile.Name,
            Handle: f.Profile.Handle ?? string.Empty,
            AvatarUrl: f.Profile.AvatarUrl,
            Bio: f.Profile.Bio,
            FollowedAt: f.FollowedAt,
            IsFollowing: followingProfileIds.Contains(f.Profile.Id)
        )).ToList();

        return new FollowersResponse(
            Followers: followerDtos,
            TotalCount: totalCount,
            NextCursor: nextCursor,
            HasMore: hasMore
        );
    }

    private static string EncodeCursor(DateTimeOffset createdAt, Guid id)
    {
        var cursor = new { CreatedAt = createdAt, Id = id };
        var json = JsonSerializer.Serialize(cursor);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes);
    }

    private static (DateTimeOffset CreatedAt, Guid Id) DecodeCursor(string cursor)
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

    private sealed record CursorData(DateTimeOffset CreatedAt, Guid Id);
}
