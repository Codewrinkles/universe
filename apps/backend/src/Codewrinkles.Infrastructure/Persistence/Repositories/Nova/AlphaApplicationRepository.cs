using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Microsoft.EntityFrameworkCore;

namespace Codewrinkles.Infrastructure.Persistence.Repositories.Nova;

public sealed class AlphaApplicationRepository : IAlphaApplicationRepository
{
    private readonly DbSet<AlphaApplication> _applications;

    public AlphaApplicationRepository(ApplicationDbContext context)
    {
        _applications = context.Set<AlphaApplication>();
    }

    public void Create(AlphaApplication application)
    {
        _applications.Add(application);
    }

    public async Task<AlphaApplication?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _applications.FindAsync([id], cancellationToken: cancellationToken);
    }

    public async Task<AlphaApplication?> FindByInviteCodeAsync(string inviteCode, CancellationToken cancellationToken)
    {
        return await _applications
            .FirstOrDefaultAsync(a => a.InviteCode == inviteCode, cancellationToken);
    }

    public async Task<AlphaApplication?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _applications
            .FirstOrDefaultAsync(a => a.Email == normalizedEmail, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _applications
            .AnyAsync(a => a.Email == normalizedEmail, cancellationToken);
    }

    public async Task<IReadOnlyList<AlphaApplication>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _applications
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AlphaApplication>> GetByStatusAsync(
        AlphaApplicationStatus status,
        CancellationToken cancellationToken)
    {
        return await _applications
            .AsNoTracking()
            .Where(a => a.Status == status)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
