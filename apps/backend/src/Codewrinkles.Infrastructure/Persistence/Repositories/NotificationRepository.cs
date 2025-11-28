using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Notification;
using Microsoft.EntityFrameworkCore;

namespace Codewrinkles.Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public void Create(Notification notification)
    {
        _context.Notifications.Add(notification);
    }

    public async Task<Notification?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Include(n => n.Actor)
            .Include(n => n.Recipient)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<(List<Notification> Notifications, int TotalCount)> GetByRecipientAsync(
        Guid recipientId,
        int offset,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications
            .Include(n => n.Actor)
            .Where(n => n.RecipientId == recipientId)
            .OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var notifications = await query
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return (notifications, totalCount);
    }

    public async Task<int> GetUnreadCountAsync(Guid recipientId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(n => n.RecipientId == recipientId && !n.IsRead)
            .CountAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(Guid recipientId, CancellationToken cancellationToken = default)
    {
        await _context.Notifications
            .Where(n => n.RecipientId == recipientId && !n.IsRead)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, DateTime.UtcNow),
                cancellationToken);
    }
}
