using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace Codewrinkles.Infrastructure.Persistence.Repositories;

public sealed class ProfileRepository : IProfileRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<Profile> _profiles;

    public ProfileRepository(ApplicationDbContext context)
    {
        _context = context;
        _profiles = context.Set<Profile>();
    }

    public Task<bool> ExistsByHandleAsync(string handle, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(handle))
            return Task.FromResult(false);

        var handleNormalized = handle.Trim().ToLowerInvariant();
        return _profiles
            .AsNoTracking()
            .AnyAsync(p => p.Handle == handleNormalized, cancellationToken);
    }

    public Task<Profile?> FindByHandleAsync(string handle, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(handle))
            return Task.FromResult<Profile?>(null);

        var handleNormalized = handle.Trim().ToLowerInvariant();
        return _profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Handle == handleNormalized, cancellationToken);
    }

    public Task<Profile?> FindByIdentityIdAsync(Guid identityId, CancellationToken cancellationToken = default)
    {
        return _profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdentityId == identityId, cancellationToken);
    }

    public async Task<Profile?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _profiles.FindAsync([id], cancellationToken: cancellationToken);
    }

    public void Create(Profile profile)
    {
        _profiles.Add(profile);
    }

    public async Task<IReadOnlyList<Profile>> SearchByHandleAsync(
        string searchTerm,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearch = searchTerm.Trim().ToLowerInvariant();

        var profiles = await _profiles
            .AsNoTracking()
            .Where(p => p.Handle != null && p.Handle.StartsWith(normalizedSearch))
            .OrderBy(p => p.Handle)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return profiles;
    }

    public async Task<IReadOnlyList<Profile>> FindByHandlesAsync(
        IEnumerable<string> handles,
        CancellationToken cancellationToken = default)
    {
        var normalizedHandles = handles
            .Select(h => h.Trim().ToLowerInvariant())
            .ToList();

        var profiles = await _profiles
            .AsNoTracking()
            .Where(p => p.Handle != null && normalizedHandles.Contains(p.Handle))
            .ToListAsync(cancellationToken);

        return profiles;
    }

    public async Task<IReadOnlyList<Profile>> GetMostFollowedProfilesAsync(
        int limit,
        Guid? excludeProfileId,
        CancellationToken cancellationToken)
    {
        // Get profiles ordered by follower count using a JOIN with Follow table
        var follows = _context.Set<Codewrinkles.Domain.Social.Follow>();

        var query = _profiles.AsNoTracking();

        // Exclude specific profile if provided (e.g., current user)
        if (excludeProfileId.HasValue)
        {
            query = query.Where(p => p.Id != excludeProfileId.Value);
        }

        var profiles = await query
            .GroupJoin(
                follows,
                profile => profile.Id,
                follow => follow.FollowingId,
                (profile, followGroup) => new { Profile = profile, FollowCount = followGroup.Count() })
            .OrderByDescending(x => x.FollowCount)
            .ThenByDescending(x => x.Profile.CreatedAt)
            .Take(limit)
            .Select(x => x.Profile)
            .ToListAsync(cancellationToken);

        return profiles;
    }
}
