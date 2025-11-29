using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse;
using Microsoft.EntityFrameworkCore;

namespace Codewrinkles.Infrastructure.Persistence.Repositories;

public sealed class HashtagRepository : IHashtagRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly DbSet<Hashtag> _hashtags;
    private readonly DbSet<PulseHashtag> _pulseHashtags;
    private readonly DbSet<Pulse> _pulses;

    public HashtagRepository(
        ApplicationDbContext context,
        IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _context = context;
        _contextFactory = contextFactory;
        _hashtags = context.Set<Hashtag>();
        _pulseHashtags = context.Set<PulseHashtag>();
        _pulses = context.Set<Pulse>();
    }

    public async Task<Hashtag?> FindByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        // Normalize tag for case-insensitive lookup
        var normalizedTag = tag.Trim().ToLowerInvariant().TrimStart('#');

        // NOTE: Not using AsNoTracking() because we need to track changes
        // when incrementing PulseCount in CreatePulse handler
        return await _hashtags
            .FirstOrDefaultAsync(h => h.Tag == normalizedTag, cancellationToken);
    }

    public async Task<List<Hashtag>> GetTrendingHashtagsAsync(int limit, CancellationToken cancellationToken = default)
    {
        return await _hashtags
            .AsNoTracking()
            .OrderByDescending(h => h.PulseCount)
            .ThenByDescending(h => h.LastUsedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public void CreateHashtag(Hashtag hashtag)
    {
        _hashtags.Add(hashtag);
    }

    public void CreatePulseHashtag(PulseHashtag pulseHashtag)
    {
        _pulseHashtags.Add(pulseHashtag);
    }

    public async Task<List<Pulse>> GetPulsesByHashtagAsync(
        string tag,
        int limit,
        DateTime? beforeCreatedAt,
        Guid? beforeId,
        CancellationToken cancellationToken = default)
    {
        // Normalize tag for lookup
        var normalizedTag = tag.Trim().ToLowerInvariant().TrimStart('#');

        // Build query with includes BEFORE select
        var query = _pulseHashtags
            .AsNoTracking()
            .Where(ph => ph.Hashtag.Tag == normalizedTag)
            .Include(ph => ph.Pulse)
                .ThenInclude(p => p.Author)
            .Include(ph => ph.Pulse)
                .ThenInclude(p => p.Engagement)
            .Include(ph => ph.Pulse)
                .ThenInclude(p => p.Image)
            .Include(ph => ph.Pulse)
                .ThenInclude(p => p.LinkPreview)
            .Include(ph => ph.Pulse)
                .ThenInclude(p => p.RepulsedPulse!)
                    .ThenInclude(rp => rp.Author)
            .Select(ph => ph.Pulse)
            .Where(p => !p.IsDeleted);

        // Cursor-based pagination
        if (beforeCreatedAt.HasValue && beforeId.HasValue)
        {
            query = query.Where(p =>
                p.CreatedAt < beforeCreatedAt.Value ||
                (p.CreatedAt == beforeCreatedAt.Value && p.Id.CompareTo(beforeId.Value) < 0));
        }

        // Apply ordering and take
        var pulses = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return pulses;
    }
}
