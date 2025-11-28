using System.Data;

namespace Codewrinkles.Application.Common.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IIdentityRepository Identities { get; }
    IProfileRepository Profiles { get; }
    IPulseRepository Pulses { get; }
    IFollowRepository Follows { get; }
    INotificationRepository Notifications { get; }
    IBookmarkRepository Bookmarks { get; }

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
