using Codewrinkles.Domain.Identity;
using Codewrinkles.Domain.Social;

namespace Codewrinkles.Application.Common.Interfaces;

public interface IFollowRepository
{
    // Queries
    Task<bool> IsFollowingAsync(
        Guid followerId,
        Guid followingId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Profile>> GetFollowersAsync(
        Guid profileId,
        int limit,
        DateTime? beforeCreatedAt,
        Guid? beforeId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Profile>> GetFollowingAsync(
        Guid profileId,
        int limit,
        DateTime? beforeCreatedAt,
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
