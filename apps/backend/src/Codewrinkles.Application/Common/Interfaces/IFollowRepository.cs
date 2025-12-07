using Codewrinkles.Domain.Identity;
using Codewrinkles.Domain.Social;

namespace Codewrinkles.Application.Common.Interfaces;

/// <summary>
/// Profile with the date it was followed. Used for pagination cursors.
/// </summary>
public sealed record ProfileWithFollowDate(Profile Profile, DateTimeOffset FollowedAt);

public interface IFollowRepository
{
    // Queries
    Task<bool> IsFollowingAsync(
        Guid followerId,
        Guid followingId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProfileWithFollowDate>> GetFollowersAsync(
        Guid profileId,
        int limit,
        DateTimeOffset? beforeCreatedAt,
        Guid? beforeId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProfileWithFollowDate>> GetFollowingAsync(
        Guid profileId,
        int limit,
        DateTimeOffset? beforeCreatedAt,
        Guid? beforeId,
        CancellationToken cancellationToken);

    Task<int> GetFollowerCountAsync(
        Guid profileId,
        CancellationToken cancellationToken);

    Task<int> GetFollowingCountAsync(
        Guid profileId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<(Profile Profile, int MutualFollowCount)>> GetSuggestedProfilesAsync(
        Guid currentUserId,
        int limit,
        CancellationToken cancellationToken);

    Task<HashSet<Guid>> GetFollowingProfileIdsAsync(
        IEnumerable<Guid> profileIds,
        Guid followerId,
        CancellationToken cancellationToken);

    Task<Follow?> FindFollowAsync(
        Guid followerId,
        Guid followingId,
        CancellationToken cancellationToken);

    // Commands
    void CreateFollow(Follow follow);
    void DeleteFollow(Follow follow);
}
