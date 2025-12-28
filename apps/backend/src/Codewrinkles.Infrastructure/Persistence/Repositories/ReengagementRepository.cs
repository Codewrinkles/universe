using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Microsoft.EntityFrameworkCore;

namespace Codewrinkles.Infrastructure.Persistence.Repositories;

public sealed class ReengagementRepository : IReengagementRepository
{
    private readonly ApplicationDbContext _context;

    public ReengagementRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<WinbackCandidate>> GetWinbackCandidatesAsync(
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        int limit,
        CancellationToken cancellationToken = default)
    {
        // Returns ALL users in the time window regardless of content
        // Includes Nova access status for differentiated email content
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
                x.Profile.Name,
                x.Profile.NovaAccess != NovaAccessLevel.None))
            .Take(limit)
            .ToListAsync(cancellationToken);

        return candidates;
    }
}
