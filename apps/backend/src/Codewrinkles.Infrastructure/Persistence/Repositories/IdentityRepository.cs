using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace Codewrinkles.Infrastructure.Persistence.Repositories;

public sealed class IdentityRepository : IIdentityRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<Identity> _identities;

    public IdentityRepository(ApplicationDbContext context)
    {
        _context = context;
        _identities = context.Set<Identity>();
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailNormalized = email.Trim().ToUpperInvariant();
        return _identities
            .AsNoTracking()
            .AnyAsync(i => i.EmailNormalized == emailNormalized, cancellationToken);
    }

    public Task<Identity?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailNormalized = email.Trim().ToUpperInvariant();
        return _identities
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.EmailNormalized == emailNormalized, cancellationToken);
    }

    public Task<Identity?> FindByEmailWithProfileAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailNormalized = email.Trim().ToUpperInvariant();
        // Tracked (no AsNoTracking) because login updates FailedLoginAttempts and LastLoginAt
        return _identities
            .Include(i => i.Profile)
            .FirstOrDefaultAsync(i => i.EmailNormalized == emailNormalized, cancellationToken);
    }

    public Task<Identity?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _identities.FindAsync([id], cancellationToken: cancellationToken).AsTask();
    }

    public void Register(Identity identity)
    {
        _identities.Add(identity);
    }
}
