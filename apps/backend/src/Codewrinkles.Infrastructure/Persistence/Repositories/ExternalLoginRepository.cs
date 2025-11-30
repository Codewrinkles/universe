using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace Codewrinkles.Infrastructure.Persistence.Repositories;

public sealed class ExternalLoginRepository : IExternalLoginRepository
{
    private readonly ApplicationDbContext _context;

    public ExternalLoginRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ExternalLogin?> FindByProviderAndUserIdAsync(
        OAuthProvider provider,
        string providerUserId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ExternalLogins
            .AsNoTracking()
            .FirstOrDefaultAsync(
                el => el.Provider == provider && el.ProviderUserId == providerUserId,
                cancellationToken);
    }

    public async Task<List<ExternalLogin>> FindByIdentityIdAsync(
        Guid identityId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ExternalLogins
            .AsNoTracking()
            .Where(el => el.IdentityId == identityId)
            .ToListAsync(cancellationToken);
    }

    public void Add(ExternalLogin externalLogin)
    {
        _context.ExternalLogins.Add(externalLogin);
    }

    public void Remove(ExternalLogin externalLogin)
    {
        _context.ExternalLogins.Remove(externalLogin);
    }
}
