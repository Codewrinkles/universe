using Codewrinkles.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Codewrinkles.Infrastructure.Persistence.Repositories;

public sealed class ReengagementRepository : IReengagementRepository
{
    private readonly ApplicationDbContext _context;

    public ReengagementRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ReengagementCandidate>> GetCandidatesAsync(
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        int limit,
        CancellationToken cancellationToken = default)
    {
        // Query users who:
        // 1. Last logged in between windowStart and windowEnd (the 24-48h window)
        // 2. Are active (not suspended)
        // 3. Have unread notifications OR new pulses from people they follow

        var candidates = await _context.Identities
            .Where(i => i.IsActive)
            .Where(i => i.LastLoginAt != null)
            .Where(i => i.LastLoginAt >= windowStart && i.LastLoginAt < windowEnd)
            .Join(
                _context.Profiles,
                i => i.Id,
                p => p.IdentityId,
                (i, p) => new { Identity = i, Profile = p })
            .Select(x => new
            {
                x.Profile.Id,
                x.Identity.Email,
                x.Profile.Name,
                x.Identity.LastLoginAt,
                UnreadNotificationCount = _context.Notifications
                    .Count(n => n.RecipientId == x.Profile.Id && !n.IsRead),
                // Count pulses from people this user follows, created after their last login
                NewPulsesFromFollowsCount = _context.Follows
                    .Where(f => f.FollowerId == x.Profile.Id)
                    .Join(
                        _context.Pulses.Where(p => p.CreatedAt > x.Identity.LastLoginAt),
                        f => f.FollowingId,
                        p => p.AuthorId,
                        (f, p) => p)
                    .Count()
            })
            // Only include users who have SOMETHING to come back to
            .Where(x => x.UnreadNotificationCount > 0 || x.NewPulsesFromFollowsCount > 0)
            // Prioritize users with notifications, then by feed activity
            .OrderByDescending(x => x.UnreadNotificationCount)
            .ThenByDescending(x => x.NewPulsesFromFollowsCount)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return candidates
            .Select(x => new ReengagementCandidate(
                x.Id,
                x.Email,
                x.Name,
                x.UnreadNotificationCount,
                x.NewPulsesFromFollowsCount))
            .ToList();
    }

    public async Task<List<WinbackCandidate>> GetWinbackCandidatesAsync(
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        int limit,
        CancellationToken cancellationToken = default)
    {
        // Simpler query for winback emails - no notification/feed counts or filter
        // Returns ALL users in the time window regardless of content
        var candidates = await _context.Identities
            .Where(i => i.IsActive)
            .Where(i => i.LastLoginAt != null)
            .Where(i => i.LastLoginAt >= windowStart && i.LastLoginAt < windowEnd)
            .Join(
                _context.Profiles,
                i => i.Id,
                p => p.IdentityId,
                (i, p) => new { Identity = i, Profile = p })
            .Select(x => new WinbackCandidate(
                x.Profile.Id,
                x.Identity.Email,
                x.Profile.Name))
            .Take(limit)
            .ToListAsync(cancellationToken);

        return candidates;
    }
}
