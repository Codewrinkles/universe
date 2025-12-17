using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Microsoft.EntityFrameworkCore;

namespace Codewrinkles.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for querying and granting automatic Nova Alpha access
/// based on Pulse activity (15+ pulses in rolling 30 days).
/// </summary>
public sealed class PulseAlphaGrantRepository : IPulseAlphaGrantRepository
{
    private readonly ApplicationDbContext _context;

    public PulseAlphaGrantRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PulseAlphaCandidate>> GetCandidatesAsync(
        int minPulseCount = 15,
        int windowDays = 30,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var windowStart = DateTimeOffset.UtcNow.AddDays(-windowDays);

        // Query users who:
        // 1. Have 15+ non-deleted pulses in the last 30 days
        // 2. Do not currently have Nova access (NovaAccess == None)
        // 3. Are active accounts (not suspended)
        var candidates = await _context.Pulses
            .Where(p => !p.IsDeleted)
            .Where(p => p.CreatedAt >= windowStart)
            .GroupBy(p => p.AuthorId)
            .Where(g => g.Count() >= minPulseCount)
            .Select(g => new
            {
                ProfileId = g.Key,
                PulseCount = g.Count()
            })
            .Join(
                _context.Profiles.Where(p => p.NovaAccess == NovaAccessLevel.None),
                g => g.ProfileId,
                p => p.Id,
                (g, p) => new { g.ProfileId, g.PulseCount, Profile = p })
            .Join(
                _context.Identities.Where(i => i.IsActive),
                x => x.Profile.IdentityId,
                i => i.Id,
                (x, i) => new PulseAlphaCandidate(
                    x.ProfileId,
                    i.Email,
                    x.Profile.Name,
                    x.PulseCount))
            .Take(limit)
            .ToListAsync(cancellationToken);

        return candidates;
    }

    public async Task GrantAlphaAccessAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var profile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.Id == profileId, cancellationToken);

        if (profile is null)
        {
            return;
        }

        profile.GrantAlphaAccess();
        await _context.SaveChangesAsync(cancellationToken);
    }
}
