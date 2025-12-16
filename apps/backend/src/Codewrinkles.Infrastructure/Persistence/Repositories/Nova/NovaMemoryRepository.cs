using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Microsoft.EntityFrameworkCore;

namespace Codewrinkles.Infrastructure.Persistence.Repositories.Nova;

public sealed class NovaMemoryRepository : INovaMemoryRepository
{
    private readonly DbSet<Memory> _memories;

    public NovaMemoryRepository(ApplicationDbContext context)
    {
        _memories = context.Set<Memory>();
    }

    public async Task<Memory?> FindByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _memories.FindAsync([id], cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<Memory>> GetActiveByProfileIdAsync(
        Guid profileId,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        return await _memories
            .AsNoTracking()
            .Where(m => m.ProfileId == profileId && m.SupersededAt == null)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Memory>> GetActiveByCategoryAsync(
        Guid profileId,
        MemoryCategory category,
        CancellationToken cancellationToken = default)
    {
        return await _memories
            .AsNoTracking()
            .Where(m => m.ProfileId == profileId
                && m.Category == category
                && m.SupersededAt == null)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Memory>> GetRecentAsync(
        Guid profileId,
        int count,
        CancellationToken cancellationToken = default)
    {
        return await _memories
            .AsNoTracking()
            .Where(m => m.ProfileId == profileId && m.SupersededAt == null)
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Memory>> GetWithEmbeddingsAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        return await _memories
            .AsNoTracking()
            .Where(m => m.ProfileId == profileId
                && m.SupersededAt == null
                && m.Embedding != null)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Memory>> GetByMinImportanceAsync(
        Guid profileId,
        int minImportance,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await _memories
            .AsNoTracking()
            .Where(m => m.ProfileId == profileId
                && m.SupersededAt == null
                && m.Importance >= minImportance)
            .OrderByDescending(m => m.Importance)
            .ThenByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<Memory?> FindActiveByCategoryAsync(
        Guid profileId,
        MemoryCategory category,
        CancellationToken cancellationToken = default)
    {
        return await _memories
            .FirstOrDefaultAsync(m => m.ProfileId == profileId
                && m.Category == category
                && m.SupersededAt == null,
                cancellationToken);
    }

    public async Task<Memory?> FindByContentAsync(
        Guid profileId,
        MemoryCategory category,
        string content,
        CancellationToken cancellationToken = default)
    {
        return await _memories
            .FirstOrDefaultAsync(m => m.ProfileId == profileId
                && m.Category == category
                && m.SupersededAt == null
                && m.Content == content,
                cancellationToken);
    }

    public void Create(Memory memory)
    {
        _memories.Add(memory);
    }

    public void Update(Memory memory)
    {
        _memories.Update(memory);
    }
}
