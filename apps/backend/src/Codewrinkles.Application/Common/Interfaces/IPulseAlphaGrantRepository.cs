namespace Codewrinkles.Application.Common.Interfaces;

/// <summary>
/// User who qualifies for automatic Alpha access via Pulse activity.
/// </summary>
public sealed record PulseAlphaCandidate(
    Guid ProfileId,
    string Email,
    string Name,
    int PulseCount);

/// <summary>
/// Repository for querying users who qualify for automatic Nova Alpha access
/// based on their Pulse activity (15+ pulses in rolling 30 days).
/// </summary>
public interface IPulseAlphaGrantRepository
{
    /// <summary>
    /// Find users who:
    /// - Have 15+ pulses in the last 30 days
    /// - Do not currently have Nova access (NovaAccess == None)
    /// - Are active accounts (not suspended)
    /// </summary>
    /// <param name="minPulseCount">Minimum number of pulses required (default: 15)</param>
    /// <param name="windowDays">Rolling window in days (default: 30)</param>
    /// <param name="limit">Maximum number of candidates to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of users who qualify for automatic Alpha access</returns>
    Task<List<PulseAlphaCandidate>> GetCandidatesAsync(
        int minPulseCount = 15,
        int windowDays = 30,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Grant Alpha access to a profile and save changes.
    /// </summary>
    /// <param name="profileId">The profile ID to grant access to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task GrantAlphaAccessAsync(Guid profileId, CancellationToken cancellationToken = default);
}
