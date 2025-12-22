using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Microsoft.EntityFrameworkCore;

namespace Codewrinkles.Infrastructure.Persistence.Repositories.Nova;

public sealed class ContentChunkRepository : IContentChunkRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<ContentChunk> _chunks;

    public ContentChunkRepository(ApplicationDbContext context)
    {
        _context = context;
        _chunks = context.Set<ContentChunk>();
    }

    public async Task<ContentChunk?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _chunks.FindAsync([id], cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<ContentChunk>> GetBySourceAsync(
        ContentSource source,
        CancellationToken cancellationToken = default)
    {
        return await _chunks
            .AsNoTracking()
            .Where(c => c.Source == source)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ContentChunk>> GetWithEmbeddingsAsync(
        ContentSource? source = null,
        string? technology = null,
        string? author = null,
        CancellationToken cancellationToken = default)
    {
        var query = _chunks.AsNoTracking();

        if (source.HasValue)
        {
            query = query.Where(c => c.Source == source.Value);
        }

        if (!string.IsNullOrWhiteSpace(technology))
        {
            query = query.Where(c => c.Technology == technology.ToLowerInvariant());
        }

        if (!string.IsNullOrWhiteSpace(author))
        {
            query = query.Where(c => c.Author == author);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsBySourceIdentifierAsync(
        ContentSource source,
        string sourceIdentifier,
        CancellationToken cancellationToken = default)
    {
        return await _chunks
            .AsNoTracking()
            .AnyAsync(c => c.Source == source && c.SourceIdentifier == sourceIdentifier, cancellationToken);
    }

    public async Task<ContentChunk?> FindBySourceIdentifierAsync(
        ContentSource source,
        string sourceIdentifier,
        CancellationToken cancellationToken = default)
    {
        return await _chunks
            .FirstOrDefaultAsync(c => c.Source == source && c.SourceIdentifier == sourceIdentifier, cancellationToken);
    }

    public async Task<IReadOnlyList<ContentChunk>> GetByParentDocumentIdAsync(
        string parentDocumentId,
        CancellationToken cancellationToken = default)
    {
        return await _chunks
            .AsNoTracking()
            .Where(c => c.ParentDocumentId == parentDocumentId)
            .OrderBy(c => c.ChunkIndex)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _chunks.CountAsync(cancellationToken);
    }

    public async Task<int> GetCountBySourceAsync(
        ContentSource source,
        CancellationToken cancellationToken = default)
    {
        return await _chunks
            .Where(c => c.Source == source)
            .CountAsync(cancellationToken);
    }

    public void Create(ContentChunk chunk)
    {
        _chunks.Add(chunk);
    }

    public void Update(ContentChunk chunk)
    {
        _chunks.Update(chunk);
    }

    public void Delete(ContentChunk chunk)
    {
        _chunks.Remove(chunk);
    }

    public async Task DeleteByParentDocumentIdAsync(
        string parentDocumentId,
        CancellationToken cancellationToken = default)
    {
        await _chunks
            .Where(c => c.ParentDocumentId == parentDocumentId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
