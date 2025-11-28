using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse;
using Microsoft.EntityFrameworkCore;

namespace Codewrinkles.Infrastructure.Persistence.Repositories;

public sealed class BookmarkRepository : IBookmarkRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<PulseBookmark> _bookmarks;

    public BookmarkRepository(ApplicationDbContext context)
    {
        _context = context;
        _bookmarks = context.Set<PulseBookmark>();
    }

    public void Create(PulseBookmark bookmark)
    {
        _bookmarks.Add(bookmark);
    }

    public void Delete(PulseBookmark bookmark)
    {
        _bookmarks.Remove(bookmark);
    }

    public async Task<PulseBookmark?> FindByProfileAndPulseAsync(
        Guid profileId,
        Guid pulseId,
        CancellationToken cancellationToken = default)
    {
        return await _bookmarks
            .FirstOrDefaultAsync(
                b => b.ProfileId == profileId && b.PulseId == pulseId,
                cancellationToken);
    }

    public async Task<bool> IsBookmarkedAsync(
        Guid profileId,
        Guid pulseId,
        CancellationToken cancellationToken = default)
    {
        return await _bookmarks
            .AsNoTracking()
            .AnyAsync(
                b => b.ProfileId == profileId && b.PulseId == pulseId,
                cancellationToken);
    }

    public async Task<HashSet<Guid>> GetBookmarkedPulseIdsAsync(
        IEnumerable<Guid> pulseIds,
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        var pulseIdList = pulseIds.ToList();

        if (pulseIdList.Count == 0)
        {
            return [];
        }

        var bookmarkedPulseIds = await _bookmarks
            .AsNoTracking()
            .Where(b => b.ProfileId == profileId && pulseIdList.Contains(b.PulseId))
            .Select(b => b.PulseId)
            .ToListAsync(cancellationToken);

        return [.. bookmarkedPulseIds];
    }

    public async Task<(List<Pulse> Pulses, bool HasMore)> GetBookmarkedPulsesAsync(
        Guid profileId,
        int limit,
        DateTime? beforeCreatedAt = null,
        Guid? beforeId = null,
        CancellationToken cancellationToken = default)
    {
        // Query bookmarks for this user, ordered by when they bookmarked (newest first)
        var query = _bookmarks
            .AsNoTracking()
            .Where(b => b.ProfileId == profileId);

        // Cursor-based pagination using bookmark creation time
        if (beforeCreatedAt.HasValue && beforeId.HasValue)
        {
            query = query.Where(b =>
                b.CreatedAt < beforeCreatedAt.Value ||
                (b.CreatedAt == beforeCreatedAt.Value && b.Id.CompareTo(beforeId.Value) < 0));
        }

        // Join with Pulses to get full pulse data
        // Eager load Author, Engagement, RepulsedPulse (for re-pulses), and Image
        // Include must come BEFORE Select to work with EF Core
        // Filter out deleted pulses in the query (not in memory)
        var bookmarks = await query
            .Where(b => !b.Pulse.IsDeleted) // Filter deleted pulses in SQL
            .Include(b => b.Pulse)
                .ThenInclude(p => p.Author)
            .Include(b => b.Pulse)
                .ThenInclude(p => p.Engagement)
            .Include(b => b.Pulse)
                .ThenInclude(p => p.RepulsedPulse)
                    .ThenInclude(rp => rp != null ? rp.Author : null)
            .Include(b => b.Pulse)
                .ThenInclude(p => p.Image)
            .OrderByDescending(b => b.CreatedAt)
            .ThenByDescending(b => b.Id)
            .Take(limit + 1) // Fetch one extra to determine if there are more
            .ToListAsync(cancellationToken);

        // Extract pulses (already filtered for non-deleted in query)
        var pulses = bookmarks
            .Select(b => b.Pulse)
            .ToList();

        var hasMore = pulses.Count > limit;

        if (hasMore)
        {
            pulses = pulses.Take(limit).ToList();
        }

        return (pulses, hasMore);
    }
}
