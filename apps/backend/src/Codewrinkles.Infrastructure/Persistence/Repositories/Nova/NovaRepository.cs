using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Microsoft.EntityFrameworkCore;

namespace Codewrinkles.Infrastructure.Persistence.Repositories.Nova;

public sealed class NovaRepository : INovaRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<ConversationSession> _sessions;
    private readonly DbSet<Message> _messages;

    public NovaRepository(ApplicationDbContext context)
    {
        _context = context;
        _sessions = context.Set<ConversationSession>();
        _messages = context.Set<Message>();
    }

    public async Task<ConversationSession?> FindSessionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _sessions.FindAsync([id], cancellationToken: cancellationToken);
    }

    public async Task<ConversationSession?> FindSessionByIdWithMessagesAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _sessions
            .AsNoTracking()
            .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationSession>> GetSessionsByProfileIdAsync(
        Guid profileId,
        int limit,
        DateTimeOffset? beforeLastMessageAt,
        Guid? beforeId,
        CancellationToken cancellationToken = default)
    {
        var query = _sessions
            .AsNoTracking()
            .Where(s => s.ProfileId == profileId && !s.IsDeleted);

        // Cursor-based pagination
        if (beforeLastMessageAt.HasValue && beforeId.HasValue)
        {
            query = query.Where(s =>
                s.LastMessageAt < beforeLastMessageAt.Value ||
                (s.LastMessageAt == beforeLastMessageAt.Value && s.Id.CompareTo(beforeId.Value) < 0));
        }

        query = query
            .OrderByDescending(s => s.LastMessageAt)
            .ThenByDescending(s => s.Id)
            .Take(limit);

        return await query.ToListAsync(cancellationToken);
    }

    public void CreateSession(ConversationSession session)
    {
        _sessions.Add(session);
    }

    public void UpdateSession(ConversationSession session)
    {
        _sessions.Update(session);
    }

    public async Task<Message?> FindMessageByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _messages.FindAsync([id], cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<Message>> GetMessagesBySessionIdAsync(
        Guid sessionId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        if (limit.HasValue)
        {
            // Get the most recent N messages, but return them in chronological order
            var recentMessages = await _messages
                .AsNoTracking()
                .Where(m => m.SessionId == sessionId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit.Value)
                .ToListAsync(cancellationToken);

            // Reverse to get chronological order
            recentMessages.Reverse();
            return recentMessages;
        }

        // Get all messages in chronological order
        return await _messages
            .AsNoTracking()
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public void CreateMessage(Message message)
    {
        _messages.Add(message);
    }

    public async Task<int> GetMessageCountBySessionIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return await _messages
            .Where(m => m.SessionId == sessionId)
            .CountAsync(cancellationToken);
    }
}
