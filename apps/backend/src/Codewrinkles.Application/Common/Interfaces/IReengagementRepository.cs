namespace Codewrinkles.Application.Common.Interfaces;

/// <summary>
/// User eligible for re-engagement email.
/// </summary>
public sealed record ReengagementCandidate(
    Guid ProfileId,
    string Email,
    string Name,
    int UnreadNotificationCount,
    int NewPulsesFromFollowsCount)
{
    /// <summary>
    /// True if user has notifications (higher priority email).
    /// </summary>
    public bool HasNotifications => UnreadNotificationCount > 0;

    /// <summary>
    /// True if user has new content from people they follow.
    /// </summary>
    public bool HasFeedUpdates => NewPulsesFromFollowsCount > 0;

    /// <summary>
    /// True if this candidate should receive any email.
    /// </summary>
    public bool ShouldReceiveEmail => HasNotifications || HasFeedUpdates;
}

public interface IReengagementRepository
{
    /// <summary>
    /// Find users who:
    /// - Last logged in between windowStart and windowEnd (24-48 hours ago)
    /// - Are active accounts (not suspended)
    /// - Have at least 1 unread notification OR new pulses from people they follow
    /// </summary>
    Task<List<ReengagementCandidate>> GetCandidatesAsync(
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        int limit,
        CancellationToken cancellationToken = default);
}
