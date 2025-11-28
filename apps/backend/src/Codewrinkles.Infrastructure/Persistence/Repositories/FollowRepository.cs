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

    public async Task<IReadOnlyList<Profile>> GetFollowersAsync(
        Guid profileId,
        int limit,
        DateTime? beforeCreatedAt,
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
            .Select(f => f.Follower)
            .ToListAsync(cancellationToken);

        return followers;
    }

    public async Task<IReadOnlyList<Profile>> GetFollowingAsync(
        Guid profileId,
        int limit,
        DateTime? beforeCreatedAt,
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
            .Select(f => f.Following)
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
        // Step 1: Get all people I'm following
        var myFollowingIds = await _follows
            .Where(f => f.FollowerId == currentUserId)
            .Select(f => f.FollowingId)
            .ToListAsync(cancellationToken);

        if (myFollowingIds.Count == 0)
        {
            // Not following anyone - return empty suggestions
            return new List<(Profile, int)>();
        }

        // Step 2: Get people followed by people I follow (2-hop)
        // Group by suggested profile and count mutual follows
        var suggestions = await _follows
            .Where(f => myFollowingIds.Contains(f.FollowerId)) // My follows' follows
            .Where(f => f.FollowingId != currentUserId)        // Exclude myself
            .Where(f => !myFollowingIds.Contains(f.FollowingId)) // Exclude people I already follow
            .GroupBy(f => f.FollowingId)
            .Select(g => new
            {
                ProfileId = g.Key,
                MutualFollowCount = g.Count() // How many of my follows also follow them
            })
            .OrderByDescending(x => x.MutualFollowCount)
            .Take(limit)
            .ToListAsync(cancellationToken);

        if (suggestions.Count == 0)
        {
            return new List<(Profile, int)>();
        }

        // Step 3: Join with profiles to get user info
        var profileIds = suggestions.Select(s => s.ProfileId).ToList();
        var profiles = await _profiles
            .Where(p => profileIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        // Step 4: Combine and return
        var result = suggestions
            .Join(profiles,
                s => s.ProfileId,
                p => p.Id,
                (s, p) => (Profile: p, MutualFollowCount: s.MutualFollowCount))
            .ToList();

        return result;
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
