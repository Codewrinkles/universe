using Codewrinkles.Domain.Identity;

namespace Codewrinkles.Application.Common.Interfaces;

public interface IExternalLoginRepository
{
    Task<ExternalLogin?> FindByProviderAndUserIdAsync(
        OAuthProvider provider,
        string providerUserId,
        CancellationToken cancellationToken = default);

    Task<List<ExternalLogin>> FindByIdentityIdAsync(
        Guid identityId,
        CancellationToken cancellationToken = default);

    void Add(ExternalLogin externalLogin);
    void Remove(ExternalLogin externalLogin);
}
