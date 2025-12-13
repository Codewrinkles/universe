using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse;
using Codewrinkles.Domain.Social;
using Microsoft.EntityFrameworkCore;

namespace Codewrinkles.Infrastructure.Persistence.Repositories;

public sealed class PulseRepository : IPulseRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly DbSet<Pulse> _pulses;
    private readonly DbSet<PulseEngagement> _engagements;
    private readonly DbSet<PulseLike> _likes;
    private readonly DbSet<PulseImage> _images;
    private readonly DbSet<PulseLinkPreview> _linkPreviews;
    private readonly DbSet<PulseMention> _mentions;
    private readonly DbSet<Follow> _follows;

    public PulseRepository(
        ApplicationDbContext context,
        IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _context = context;
        _contextFactory = contextFactory;
        _pulses = context.Set<Pulse>();
        _engagements = context.Set<PulseEngagement>();
        _likes = context.Set<PulseLike>();
        _images = context.Set<PulseImage>();
        _linkPreviews = context.Set<PulseLinkPreview>();
        _mentions = context.Set<PulseMention>();
        _follows = context.Set<Follow>();
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
            .Include(p => p.LinkPreview)
            .Include(p => p.RepulsedPulse)
                .ThenInclude(rp => rp!.Author)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Pulse>> GetFeedAsync(
        Guid? currentUserId,
        int limit,
        DateTimeOffset? beforeCreatedAt,
        Guid? beforeId,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Pulse> query;

        if (currentUserId.HasValue)
        {
            // AUTHENTICATED USER: Show pulses from followed users + own pulses

            // Step 1: Get IDs of users I'm following
            var followingIds = await _follows
                .Where(f => f.FollowerId == currentUserId.Value)
                .Select(f => f.FollowingId)
                .ToListAsync(cancellationToken);

            // Step 2: Check if user is following anyone
            if (followingIds.Count == 0)
            {
                // NOT FOLLOWING ANYONE: Show public feed (all pulses)
                // This solves the "cold start problem" for new users
                query = _pulses
                    .AsNoTracking()
                    .Include(p => p.Author)
                    .Include(p => p.Engagement)
                    .Include(p => p.Image)
                    .Include(p => p.LinkPreview)
                    .Include(p => p.RepulsedPulse)
                        .ThenInclude(rp => rp!.Author)
                    .Include(p => p.ParentPulse)
                        .ThenInclude(parent => parent!.Author)
                    .Where(p => !p.IsDeleted);
            }
            else
            {
                // FOLLOWING USERS: Show personalized feed (followed users + own pulses)

                // Include my own ID (see my own pulses)
                followingIds.Add(currentUserId.Value);

                // Get pulses from followed users + myself
                query = _pulses
                    .AsNoTracking()
                    .Include(p => p.Author)
                    .Include(p => p.Engagement)
                    .Include(p => p.Image)
                    .Include(p => p.LinkPreview)
                    .Include(p => p.RepulsedPulse)
                        .ThenInclude(rp => rp!.Author)
                    .Include(p => p.ParentPulse)
                        .ThenInclude(parent => parent!.Author)
                    .Where(p => !p.IsDeleted && followingIds.Contains(p.AuthorId));
            }
        }
        else
        {
            // UNAUTHENTICATED USER: Show all pulses (public feed)
            query = _pulses
                .AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.Engagement)
                .Include(p => p.Image)
                .Include(p => p.LinkPreview)
                .Include(p => p.RepulsedPulse)
                    .ThenInclude(rp => rp!.Author)
                .Include(p => p.ParentPulse)
                    .ThenInclude(parent => parent!.Author)
                .Where(p => !p.IsDeleted);
        }

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

        return await query.ToListAsync<Pulse>(cancellationToken);
    }

    public Task<IReadOnlyList<Pulse>> GetByAuthorIdAsync(
        Guid authorId,
        int limit,
        DateTimeOffset? beforeCreatedAt,
        Guid? beforeId,
        CancellationToken cancellationToken = default)
    {
        var query = _pulses
            .AsNoTracking()
            .Include(p => p.Author)
            .Include(p => p.Engagement)
            .Include(p => p.Image)
            .Include(p => p.LinkPreview)
            .Include(p => p.RepulsedPulse)
                .ThenInclude(rp => rp!.Author)
            .Include(p => p.ParentPulse)
                .ThenInclude(parent => parent!.Author)
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

    public void CreateLinkPreview(PulseLinkPreview linkPreview)
    {
        _linkPreviews.Add(linkPreview);
    }

    public Task<IReadOnlyList<Pulse>> GetRepliesByThreadRootIdAsync(
        Guid threadRootId,
        int limit,
        DateTimeOffset? beforeCreatedAt,
        Guid? beforeId,
        CancellationToken cancellationToken = default)
    {
        var query = _pulses
            .AsNoTracking()
            .Include(p => p.Author)
            .Include(p => p.Engagement)
            .Include(p => p.Image)
            .Include(p => p.LinkPreview)
            .Include(p => p.ParentPulse)
                .ThenInclude(parent => parent!.Author)
            .Where(p => p.ThreadRootId == threadRootId && !p.IsDeleted);

        // Cursor-based pagination (chronological order - oldest first for threads)
        if (beforeCreatedAt.HasValue && beforeId.HasValue)
        {
            query = query.Where(p =>
                p.CreatedAt > beforeCreatedAt.Value ||
                (p.CreatedAt == beforeCreatedAt.Value && p.Id.CompareTo(beforeId.Value) > 0));
        }

        query = query
            .OrderBy(p => p.CreatedAt)  // ASC for chronological thread reading
            .ThenBy(p => p.Id)
            .Take(limit);

        return query.ToListAsync<Pulse>(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<Pulse>)t.Result, cancellationToken);
    }

    public Task<int> GetReplyCountByThreadRootIdAsync(
        Guid threadRootId,
        CancellationToken cancellationToken = default)
    {
        return _pulses
            .AsNoTracking()
            .CountAsync(p => p.ThreadRootId == threadRootId && !p.IsDeleted, cancellationToken);
    }

    public void CreateMention(PulseMention mention)
    {
        _mentions.Add(mention);
    }

    public async Task<IReadOnlyList<PulseMention>> GetMentionsForPulsesAsync(
        IEnumerable<Guid> pulseIds,
        CancellationToken cancellationToken = default)
    {
        var pulseIdList = pulseIds.ToList();

        if (pulseIdList.Count == 0)
        {
            return [];
        }

        var mentions = await _mentions
            .AsNoTracking()
            .Where(m => pulseIdList.Contains(m.PulseId))
            .ToListAsync(cancellationToken);

        return mentions;
    }

    public async Task<FeedData> GetFeedWithMetadataAsync(
        Guid? currentUserId,
        int limit,
        DateTimeOffset? beforeCreatedAt,
        Guid? beforeId,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Get pulses using the existing method (uses scoped DbContext)
        var pulses = await GetFeedAsync(
            currentUserId,
            limit,
            beforeCreatedAt,
            beforeId,
            cancellationToken);

        // Step 2: If no user or no pulses, return early with empty metadata
        if (!currentUserId.HasValue || pulses.Count == 0)
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
        var userId = currentUserId.Value;

        // Step 4: Execute metadata queries in parallel using factory-created DbContexts
        // Each query gets its own isolated DbContext instance
        var likesTask = Task.Run(async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var likedPulseIds = await context.Set<PulseLike>()
                .AsNoTracking()
                .Where(l => l.ProfileId == userId && pulseIds.Contains(l.PulseId))
                .Select(l => l.PulseId)
                .ToListAsync(cancellationToken);
            return likedPulseIds.ToHashSet();
        }, cancellationToken);

        var bookmarksTask = Task.Run(async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var bookmarkedPulseIds = await context.Set<PulseBookmark>()
                .AsNoTracking()
                .Where(b => b.ProfileId == userId && pulseIds.Contains(b.PulseId))
                .Select(b => b.PulseId)
                .ToListAsync(cancellationToken);
            return bookmarkedPulseIds.ToHashSet();
        }, cancellationToken);

        var followingTask = Task.Run(async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var followingProfileIds = await context.Set<Follow>()
                .AsNoTracking()
                .Where(f => f.FollowerId == userId && authorIds.Contains(f.FollowingId))
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

    public async Task<FeedData> GetPulsesByAuthorWithMetadataAsync(
        Guid authorId,
        Guid? currentUserId,
        int limit,
        DateTimeOffset? beforeCreatedAt,
        Guid? beforeId,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Get pulses using the existing method (uses scoped DbContext)
        var pulses = await GetByAuthorIdAsync(
            authorId,
            limit,
            beforeCreatedAt,
            beforeId,
            cancellationToken);

        // Step 2: If no user or no pulses, return early with empty metadata
        if (!currentUserId.HasValue || pulses.Count == 0)
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
        var userId = currentUserId.Value;

        // Step 4: Execute metadata queries in parallel using factory-created DbContexts
        var likesTask = Task.Run(async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var likedPulseIds = await context.Set<PulseLike>()
                .AsNoTracking()
                .Where(l => l.ProfileId == userId && pulseIds.Contains(l.PulseId))
                .Select(l => l.PulseId)
                .ToListAsync(cancellationToken);
            return likedPulseIds.ToHashSet();
        }, cancellationToken);

        var bookmarksTask = Task.Run(async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var bookmarkedPulseIds = await context.Set<PulseBookmark>()
                .AsNoTracking()
                .Where(b => b.ProfileId == userId && pulseIds.Contains(b.PulseId))
                .Select(b => b.PulseId)
                .ToListAsync(cancellationToken);
            return bookmarkedPulseIds.ToHashSet();
        }, cancellationToken);

        var followingTask = Task.Run(async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var followingProfileIds = await context.Set<Follow>()
                .AsNoTracking()
                .Where(f => f.FollowerId == userId && authorIds.Contains(f.FollowingId))
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

    public async Task<int> GetPulseCountByAuthorAsync(
        Guid authorId,
        CancellationToken cancellationToken)
    {
        return await _pulses
            .Where(p => p.AuthorId == authorId && !p.IsDeleted)
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken)
    {
        return await _pulses
            .Where(p => !p.IsDeleted)
            .CountAsync(cancellationToken);
    }

    public async Task DeleteMentionsForPulseAsync(Guid pulseId, CancellationToken cancellationToken)
    {
        var mentions = await _mentions
            .Where(m => m.PulseId == pulseId)
            .ToListAsync(cancellationToken);

        _mentions.RemoveRange(mentions);
    }
}
