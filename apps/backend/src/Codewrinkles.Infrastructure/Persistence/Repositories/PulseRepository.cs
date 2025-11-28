using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse;
using Microsoft.EntityFrameworkCore;

namespace Codewrinkles.Infrastructure.Persistence.Repositories;

public sealed class PulseRepository : IPulseRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<Pulse> _pulses;
    private readonly DbSet<PulseEngagement> _engagements;
    private readonly DbSet<PulseLike> _likes;
    private readonly DbSet<PulseImage> _images;

    public PulseRepository(ApplicationDbContext context)
    {
        _context = context;
        _pulses = context.Set<Pulse>();
        _engagements = context.Set<PulseEngagement>();
        _likes = context.Set<PulseLike>();
        _images = context.Set<PulseImage>();
    }

    public async Task<Pulse?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _pulses.FindAsync([id], cancellationToken: cancellationToken);
    }

    public Task<Pulse?> FindByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _pulses
            .AsNoTracking()
            .Include(p => p.Author)
            .Include(p => p.Engagement)
            .Include(p => p.Image)
            .Include(p => p.RepulsedPulse)
                .ThenInclude(rp => rp!.Author)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public Task<IReadOnlyList<Pulse>> GetFeedAsync(
        int limit,
        DateTime? beforeCreatedAt,
        Guid? beforeId,
        CancellationToken cancellationToken = default)
    {
        var query = _pulses
            .AsNoTracking()
            .Include(p => p.Author)
            .Include(p => p.Engagement)
            .Include(p => p.Image)
            .Include(p => p.RepulsedPulse)
                .ThenInclude(rp => rp!.Author)
            .Where(p => !p.IsDeleted);

        // Cursor-based pagination
        if (beforeCreatedAt.HasValue && beforeId.HasValue)
        {
            query = query.Where(p =>
                p.CreatedAt < beforeCreatedAt.Value ||
                (p.CreatedAt == beforeCreatedAt.Value && p.Id.CompareTo(beforeId.Value) < 0));
        }

        query = query
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Take(limit);

        return query.ToListAsync<Pulse>(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<Pulse>)t.Result, cancellationToken);
    }

    public Task<IReadOnlyList<Pulse>> GetByAuthorIdAsync(
        Guid authorId,
        int limit,
        DateTime? beforeCreatedAt,
        Guid? beforeId,
        CancellationToken cancellationToken = default)
    {
        var query = _pulses
            .AsNoTracking()
            .Include(p => p.Engagement)
            .Include(p => p.Image)
            .Include(p => p.RepulsedPulse)
                .ThenInclude(rp => rp!.Author)
            .Where(p => p.AuthorId == authorId && !p.IsDeleted);

        // Cursor-based pagination
        if (beforeCreatedAt.HasValue && beforeId.HasValue)
        {
            query = query.Where(p =>
                p.CreatedAt < beforeCreatedAt.Value ||
                (p.CreatedAt == beforeCreatedAt.Value && p.Id.CompareTo(beforeId.Value) < 0));
        }

        query = query
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Take(limit);

        return query.ToListAsync<Pulse>(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<Pulse>)t.Result, cancellationToken);
    }

    public void Create(Pulse pulse)
    {
        _pulses.Add(pulse);
    }

    public void CreateEngagement(PulseEngagement engagement)
    {
        _engagements.Add(engagement);
    }

    public async Task<PulseEngagement?> FindEngagementAsync(Guid pulseId, CancellationToken cancellationToken = default)
    {
        return await _engagements.FindAsync([pulseId], cancellationToken: cancellationToken);
    }

    public void Delete(Pulse pulse)
    {
        // Soft delete - mark as deleted (domain entity handles this)
        // No need to call Remove, just update the entity
        _pulses.Update(pulse);
    }

    public async Task<PulseLike?> FindLikeAsync(
        Guid pulseId,
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        return await _likes.FindAsync([pulseId, profileId], cancellationToken: cancellationToken);
    }

    public Task<bool> HasUserLikedPulseAsync(
        Guid pulseId,
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        return _likes.AnyAsync(
            l => l.PulseId == pulseId && l.ProfileId == profileId,
            cancellationToken);
    }

    public async Task<HashSet<Guid>> GetLikedPulseIdsAsync(
        IEnumerable<Guid> pulseIds,
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        var pulseIdList = pulseIds.ToList();

        if (pulseIdList.Count == 0)
        {
            return [];
        }

        var likedPulseIds = await _likes
            .AsNoTracking()
            .Where(l => l.ProfileId == profileId && pulseIdList.Contains(l.PulseId))
            .Select(l => l.PulseId)
            .ToListAsync(cancellationToken);

        return [.. likedPulseIds];
    }

    public void CreateLike(PulseLike like)
    {
        _likes.Add(like);
    }

    public void DeleteLike(PulseLike like)
    {
        _likes.Remove(like);
    }

    public void CreateImage(PulseImage image)
    {
        _images.Add(image);
    }
}
