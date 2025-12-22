using System.Data;
using Codewrinkles.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Codewrinkles.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly IIdentityRepository _identities;
    private readonly IProfileRepository _profiles;
    private readonly IExternalLoginRepository _externalLogins;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPulseRepository _pulses;
    private readonly IFollowRepository _follows;
    private readonly INotificationRepository _notifications;
    private readonly IBookmarkRepository _bookmarks;
    private readonly IHashtagRepository _hashtags;
    private readonly INovaRepository _nova;
    private readonly INovaMemoryRepository _novaMemories;
    private readonly IAlphaApplicationRepository _alphaApplications;
    private readonly IContentChunkRepository _contentChunks;
    private readonly IContentIngestionJobRepository _contentIngestionJobs;

    public UnitOfWork(
        ApplicationDbContext context,
        IIdentityRepository identities,
        IProfileRepository profiles,
        IExternalLoginRepository externalLogins,
        IRefreshTokenRepository refreshTokens,
        IPulseRepository pulses,
        IFollowRepository follows,
        INotificationRepository notifications,
        IBookmarkRepository bookmarks,
        IHashtagRepository hashtags,
        INovaRepository nova,
        INovaMemoryRepository novaMemories,
        IAlphaApplicationRepository alphaApplications,
        IContentChunkRepository contentChunks,
        IContentIngestionJobRepository contentIngestionJobs)
    {
        _context = context;
        _identities = identities;
        _profiles = profiles;
        _externalLogins = externalLogins;
        _refreshTokens = refreshTokens;
        _pulses = pulses;
        _follows = follows;
        _notifications = notifications;
        _bookmarks = bookmarks;
        _hashtags = hashtags;
        _nova = nova;
        _novaMemories = novaMemories;
        _alphaApplications = alphaApplications;
        _contentChunks = contentChunks;
        _contentIngestionJobs = contentIngestionJobs;
    }

    public IIdentityRepository Identities => _identities;

    public IProfileRepository Profiles => _profiles;

    public IExternalLoginRepository ExternalLogins => _externalLogins;

    public IRefreshTokenRepository RefreshTokens => _refreshTokens;

    public IPulseRepository Pulses => _pulses;

    public IFollowRepository Follows => _follows;

    public INotificationRepository Notifications => _notifications;

    public IBookmarkRepository Bookmarks => _bookmarks;

    public IHashtagRepository Hashtags => _hashtags;

    public INovaRepository Nova => _nova;

    public INovaMemoryRepository NovaMemories => _novaMemories;

    public IAlphaApplicationRepository AlphaApplications => _alphaApplications;

    public IContentChunkRepository ContentChunks => _contentChunks;

    public IContentIngestionJobRepository ContentIngestionJobs => _contentIngestionJobs;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);

        return new UnitOfWorkTransaction(transaction);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}

public sealed class UnitOfWorkTransaction : IUnitOfWorkTransaction
{
    private readonly IDbContextTransaction _transaction;

    public UnitOfWorkTransaction(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return _transaction.CommitAsync(cancellationToken);
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        return _transaction.RollbackAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _transaction.DisposeAsync();
    }
}
