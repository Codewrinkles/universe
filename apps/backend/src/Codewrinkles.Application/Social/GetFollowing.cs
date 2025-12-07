using System.Text;
using System.Text.Json;
using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Social;

public sealed record GetFollowingQuery(
    Guid ProfileId,
    Guid? CurrentUserId,
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
        DateTimeOffset? beforeCreatedAt = null;
        Guid? beforeId = null;

        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            var cursor = DecodeCursor(query.Cursor);
            beforeCreatedAt = cursor.CreatedAt;
            beforeId = cursor.Id;
        }

        // Fetch following from repository (now returns ProfileWithFollowDate)
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

        // Generate next cursor using the actual FollowedAt from the Follow entity
        string? nextCursor = null;
        if (hasMore)
        {
            var lastFollowing = followingToReturn.Last();
            nextCursor = EncodeCursor(lastFollowing.FollowedAt, lastFollowing.Profile.Id);
        }

        // Batch check which following the current user is also following (if authenticated)
        var followingProfileIds = new HashSet<Guid>();
        if (query.CurrentUserId.HasValue)
        {
            var profileIds = followingToReturn.Select(f => f.Profile.Id).ToList();
            followingProfileIds = await _unitOfWork.Follows.GetFollowingProfileIdsAsync(
                profileIds,
                query.CurrentUserId.Value,
                cancellationToken);
        }

        // Map to DTOs - now using the actual FollowedAt from the Follow entity
        var followingDtos = followingToReturn.Select(f => new FollowingDto(
            ProfileId: f.Profile.Id,
            Name: f.Profile.Name,
            Handle: f.Profile.Handle ?? string.Empty,
            AvatarUrl: f.Profile.AvatarUrl,
            Bio: f.Profile.Bio,
            FollowedAt: f.FollowedAt,
            IsFollowing: followingProfileIds.Contains(f.Profile.Id)
        )).ToList();

        return new FollowingResponse(
            Following: followingDtos,
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
