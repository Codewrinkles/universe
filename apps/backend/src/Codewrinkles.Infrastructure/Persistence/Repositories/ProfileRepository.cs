using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace Codewrinkles.Infrastructure.Persistence.Repositories;

public sealed class ProfileRepository : IProfileRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<Profile> _profiles;

    public ProfileRepository(ApplicationDbContext context)
    {
        _context = context;
        _profiles = context.Set<Profile>();
    }

    public Task<bool> ExistsByHandleAsync(string handle, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(handle))
            return Task.FromResult(false);

        var handleNormalized = handle.Trim().ToLowerInvariant();
        return _profiles
            .AsNoTracking()
            .AnyAsync(p => p.Handle == handleNormalized, cancellationToken);
    }

    public Task<Profile?> FindByHandleAsync(string handle, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(handle))
            return Task.FromResult<Profile?>(null);

        var handleNormalized = handle.Trim().ToLowerInvariant();
        return _profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Handle == handleNormalized, cancellationToken);
    }

    public Task<Profile?> FindByIdentityIdAsync(Guid identityId, CancellationToken cancellationToken = default)
    {
        return _profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdentityId == identityId, cancellationToken);
    }

    public Task<Profile?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _profiles.FindAsync([id], cancellationToken: cancellationToken).AsTask();
    }

    public void Create(Profile profile)
    {
        _profiles.Add(profile);
    }
}
