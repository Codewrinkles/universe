using Codewrinkles.Domain.Nova;

namespace Codewrinkles.Application.Common.Interfaces;

public interface INovaRepository
{
    // ConversationSession operations

    /// <summary>
    /// Find conversation session by ID.
    /// Returns null if not found.
    /// </summary>
    Task<ConversationSession?> FindSessionByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find conversation session by ID with all messages eagerly loaded.
    /// Messages are ordered by CreatedAt ascending.
    /// Returns null if not found.
    /// </summary>
    Task<ConversationSession?> FindSessionByIdWithMessagesAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paginated list of conversation sessions for a profile.
    /// Excludes deleted sessions, ordered by LastMessageAt descending.
    /// </summary>
    Task<IReadOnlyList<ConversationSession>> GetSessionsByProfileIdAsync(
        Guid profileId,
        int limit,
        DateTimeOffset? beforeLastMessageAt,
        Guid? beforeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new conversation session.
    /// Does not save to database - call UnitOfWork.SaveChangesAsync().
    /// </summary>
    void CreateSession(ConversationSession session);

    /// <summary>
    /// Update a conversation session.
    /// Does not save to database - call UnitOfWork.SaveChangesAsync().
    /// </summary>
    void UpdateSession(ConversationSession session);

    // Message operations

    /// <summary>
    /// Find message by ID.
    /// Returns null if not found.
    /// </summary>
    Task<Message?> FindMessageByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get messages for a conversation session, ordered by CreatedAt ascending.
    /// Optionally limit to most recent N messages.
    /// </summary>
    Task<IReadOnlyList<Message>> GetMessagesBySessionIdAsync(
        Guid sessionId,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new message.
    /// Does not save to database - call UnitOfWork.SaveChangesAsync().
    /// </summary>
    void CreateMessage(Message message);

    /// <summary>
    /// Get the count of messages in a conversation session.
    /// </summary>
    Task<int> GetMessageCountBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);

    // LearnerProfile operations

    /// <summary>
    /// Find learner profile by ID.
    /// Returns null if not found.
    /// </summary>
    Task<LearnerProfile?> FindLearnerProfileByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find learner profile by the global Profile ID.
    /// Returns null if not found.
    /// </summary>
    Task<LearnerProfile?> FindLearnerProfileByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new learner profile.
    /// Does not save to database - call UnitOfWork.SaveChangesAsync().
    /// </summary>
    void CreateLearnerProfile(LearnerProfile learnerProfile);

    /// <summary>
    /// Update a learner profile.
    /// Does not save to database - call UnitOfWork.SaveChangesAsync().
    /// </summary>
    void UpdateLearnerProfile(LearnerProfile learnerProfile);
}
