using System.Data;

namespace Codewrinkles.Application.Common.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IIdentityRepository Identities { get; }
    IProfileRepository Profiles { get; }
    IExternalLoginRepository ExternalLogins { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IPulseRepository Pulses { get; }
    IFollowRepository Follows { get; }
    INotificationRepository Notifications { get; }
    IBookmarkRepository Bookmarks { get; }
    IHashtagRepository Hashtags { get; }
    INovaRepository Nova { get; }
    INovaMemoryRepository NovaMemories { get; }
    IAlphaApplicationRepository AlphaApplications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken);
}

public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}
