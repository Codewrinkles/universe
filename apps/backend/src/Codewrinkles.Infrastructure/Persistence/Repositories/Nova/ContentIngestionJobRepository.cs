using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Microsoft.EntityFrameworkCore;

namespace Codewrinkles.Infrastructure.Persistence.Repositories.Nova;

public sealed class ContentIngestionJobRepository : IContentIngestionJobRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<ContentIngestionJob> _jobs;

    public ContentIngestionJobRepository(ApplicationDbContext context)
    {
        _context = context;
        _jobs = context.Set<ContentIngestionJob>();
    }

    public async Task<ContentIngestionJob?> FindByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _jobs.FindAsync([id], cancellationToken: cancellationToken);
    }

    public async Task<ContentIngestionJob?> FindByParentDocumentIdAsync(
        string parentDocumentId,
        CancellationToken cancellationToken = default)
    {
        return await _jobs
            .FirstOrDefaultAsync(j => j.ParentDocumentId == parentDocumentId, cancellationToken);
    }

    public async Task<IReadOnlyList<ContentIngestionJob>> GetAllAsync(
        IngestionJobStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _jobs.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(j => j.Status == status.Value);
        }

        return await query
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ContentIngestionJob>> GetQueuedJobsAsync(
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        return await _jobs
            .AsNoTracking()
            .Where(j => j.Status == IngestionJobStatus.Queued)
            .OrderBy(j => j.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public void Create(ContentIngestionJob job)
    {
        _jobs.Add(job);
    }

    public void Update(ContentIngestionJob job)
    {
        _jobs.Update(job);
    }

    public void Delete(ContentIngestionJob job)
    {
        _jobs.Remove(job);
    }
}
