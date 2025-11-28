using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse;
using Codewrinkles.Domain.Social;
using Microsoft.EntityFrameworkCore;

namespace Codewrinkles.Infrastructure.Persistence.Repositories;

public sealed class BookmarkRepository : IBookmarkRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly DbSet<PulseBookmark> _bookmarks;

    public BookmarkRepository(
        ApplicationDbContext context,
        IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _context = context;
        _contextFactory = contextFactory;
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

    public async Task<FeedData> GetBookmarkedPulsesWithMetadataAsync(
        Guid profileId,
        int limit,
        DateTime? beforeCreatedAt = null,
        Guid? beforeId = null,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Get bookmarked pulses using the existing method (uses scoped DbContext)
        var (pulses, _) = await GetBookmarkedPulsesAsync(
            profileId,
            limit,
            beforeCreatedAt,
            beforeId,
            cancellationToken);

        // Step 2: If no pulses, return early with empty metadata
        if (pulses.Count == 0)
        {
            return new FeedData(
                Pulses: pulses,
                LikedPulseIds: [],
                BookmarkedPulseIds: [],
                FollowingProfileIds: [],
                Mentions: []
            );
        }

        // Step 3: Prepare data for parallel queries
        var pulseIds = pulses.Select(p => p.Id).ToList();
        var authorIds = pulses.Select(p => p.AuthorId).Distinct().ToList();

        // Step 4: Execute metadata queries in parallel using factory-created DbContexts
        // Note: All pulses are bookmarked by definition, so we just get their IDs
        var likesTask = Task.Run(async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var likedPulseIds = await context.Set<PulseLike>()
                .AsNoTracking()
                .Where(l => l.ProfileId == profileId && pulseIds.Contains(l.PulseId))
                .Select(l => l.PulseId)
                .ToListAsync(cancellationToken);
            return likedPulseIds.ToHashSet();
        }, cancellationToken);

        var bookmarksTask = Task.Run(async () =>
        {
            // All pulses are bookmarked, just return the pulse IDs
            return pulseIds.ToHashSet();
        }, cancellationToken);

        var followingTask = Task.Run(async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var followingProfileIds = await context.Set<Follow>()
                .AsNoTracking()
                .Where(f => f.FollowerId == profileId && authorIds.Contains(f.FollowingId))
                .Select(f => f.FollowingId)
                .ToListAsync(cancellationToken);
            return followingProfileIds.ToHashSet();
        }, cancellationToken);

        var mentionsTask = Task.Run(async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var mentions = await context.Set<PulseMention>()
                .AsNoTracking()
                .Where(m => pulseIds.Contains(m.PulseId))
                .ToListAsync(cancellationToken);
            return (IReadOnlyList<PulseMention>)mentions;
        }, cancellationToken);

        // Step 5: Wait for all parallel queries to complete
        await Task.WhenAll(likesTask, bookmarksTask, followingTask, mentionsTask);

        // Step 6: Return aggregated results
        return new FeedData(
            Pulses: pulses,
            LikedPulseIds: await likesTask,
            BookmarkedPulseIds: await bookmarksTask,
            FollowingProfileIds: await followingTask,
            Mentions: await mentionsTask
        );
    }
}
