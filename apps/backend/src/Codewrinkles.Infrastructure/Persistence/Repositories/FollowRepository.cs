using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Identity;
using Codewrinkles.Domain.Social;
using Microsoft.EntityFrameworkCore;

namespace Codewrinkles.Infrastructure.Persistence.Repositories;

public sealed class FollowRepository : IFollowRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<Follow> _follows;
    private readonly DbSet<Profile> _profiles;

    public FollowRepository(ApplicationDbContext context)
    {
        _context = context;
        _follows = context.Set<Follow>();
        _profiles = context.Set<Profile>();
    }

    public Task<bool> IsFollowingAsync(
        Guid followerId,
        Guid followingId,
        CancellationToken cancellationToken)
    {
        return _follows
            .AsNoTracking()
            .AnyAsync(
                f => f.FollowerId == followerId && f.FollowingId == followingId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ProfileWithFollowDate>> GetFollowersAsync(
        Guid profileId,
        int limit,
        DateTimeOffset? beforeCreatedAt,
        Guid? beforeId,
        CancellationToken cancellationToken)
    {
        var query = _follows
            .AsNoTracking()
            .Where(f => f.FollowingId == profileId);

        // Cursor-based pagination
        if (beforeCreatedAt.HasValue && beforeId.HasValue)
        {
            query = query.Where(f =>
                f.CreatedAt < beforeCreatedAt.Value ||
                (f.CreatedAt == beforeCreatedAt.Value && f.FollowerId.CompareTo(beforeId.Value) < 0));
        }

        var followers = await query
            .OrderByDescending(f => f.CreatedAt)
            .ThenByDescending(f => f.FollowerId)
            .Take(limit)
            .Select(f => new ProfileWithFollowDate(f.Follower, f.CreatedAt))
            .ToListAsync(cancellationToken);

        return followers;
    }

    public async Task<IReadOnlyList<ProfileWithFollowDate>> GetFollowingAsync(
        Guid profileId,
        int limit,
        DateTimeOffset? beforeCreatedAt,
        Guid? beforeId,
        CancellationToken cancellationToken)
    {
        var query = _follows
            .AsNoTracking()
            .Where(f => f.FollowerId == profileId);

        // Cursor-based pagination
        if (beforeCreatedAt.HasValue && beforeId.HasValue)
        {
            query = query.Where(f =>
                f.CreatedAt < beforeCreatedAt.Value ||
                (f.CreatedAt == beforeCreatedAt.Value && f.FollowingId.CompareTo(beforeId.Value) < 0));
        }

        var following = await query
            .OrderByDescending(f => f.CreatedAt)
            .ThenByDescending(f => f.FollowingId)
            .Take(limit)
            .Select(f => new ProfileWithFollowDate(f.Following, f.CreatedAt))
            .ToListAsync(cancellationToken);

        return following;
    }

    public Task<int> GetFollowerCountAsync(
        Guid profileId,
        CancellationToken cancellationToken)
    {
        return _follows
            .AsNoTracking()
            .CountAsync(f => f.FollowingId == profileId, cancellationToken);
    }

    public Task<int> GetFollowingCountAsync(
        Guid profileId,
        CancellationToken cancellationToken)
    {
        return _follows
            .AsNoTracking()
            .CountAsync(f => f.FollowerId == profileId, cancellationToken);
    }

    public async Task<IReadOnlyList<(Profile Profile, int MutualFollowCount)>> GetSuggestedProfilesAsync(
        Guid currentUserId,
        int limit,
        CancellationToken cancellationToken)
    {
        // Optimized single-query approach using subqueries instead of multiple roundtrips
        // This executes as a single SQL query with joins and subqueries

        // Subquery: People I'm following
        var myFollowingQuery = _follows
            .Where(f => f.FollowerId == currentUserId)
            .Select(f => f.FollowingId);

        // Main query: Get suggested profiles with mutual follow counts
        // This translates to a single SQL query with JOINs and GROUP BY
        var suggestions = await _follows
            .Where(f => myFollowingQuery.Contains(f.FollowerId))       // Their followers are people I follow (2-hop)
            .Where(f => f.FollowingId != currentUserId)                // Exclude myself
            .Where(f => !myFollowingQuery.Contains(f.FollowingId))     // Exclude people I already follow
            .GroupBy(f => f.Following)                                  // Group by the Profile entity (not just ID)
            .Select(g => new
            {
                Profile = g.Key,                                        // The Profile entity
                MutualFollowCount = g.Count()                           // How many mutual follows
            })
            .OrderByDescending(x => x.MutualFollowCount)
            .Take(limit)
            .ToListAsync(cancellationToken);

        // Map to result tuple
        var result = suggestions
            .Select(s => (Profile: s.Profile, MutualFollowCount: s.MutualFollowCount))
            .ToList();

        return result;
    }

    public async Task<HashSet<Guid>> GetFollowingProfileIdsAsync(
        IEnumerable<Guid> profileIds,
        Guid followerId,
        CancellationToken cancellationToken)
    {
        var profileIdList = profileIds.ToList();

        if (profileIdList.Count == 0)
        {
            return [];
        }

        var followingProfileIds = await _follows
            .AsNoTracking()
            .Where(f => f.FollowerId == followerId && profileIdList.Contains(f.FollowingId))
            .Select(f => f.FollowingId)
            .ToListAsync(cancellationToken);

        return [.. followingProfileIds];
    }

    public Task<Follow?> FindFollowAsync(
        Guid followerId,
        Guid followingId,
        CancellationToken cancellationToken)
    {
        return _follows
            .AsNoTracking()
            .FirstOrDefaultAsync(
                f => f.FollowerId == followerId && f.FollowingId == followingId,
                cancellationToken);
    }

    public void CreateFollow(Follow follow)
    {
        _follows.Add(follow);
    }

    public void DeleteFollow(Follow follow)
    {
        _follows.Remove(follow);
    }
}
