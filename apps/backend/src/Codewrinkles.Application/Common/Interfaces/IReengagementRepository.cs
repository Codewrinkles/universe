namespace Codewrinkles.Application.Common.Interfaces;

/// <summary>
/// User eligible for winback email.
/// </summary>
public sealed record WinbackCandidate(
    Guid ProfileId,
    string Email,
    string Name,
    bool HasNovaAccess);

public interface IReengagementRepository
{
    /// <summary>
    /// Find users who:
    /// - Last logged in between windowStart and windowEnd
    /// - Are active accounts (not suspended)
    /// - No content filter - returns ALL users in window (for winback emails)
    /// </summary>
    Task<List<WinbackCandidate>> GetWinbackCandidatesAsync(
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        int limit,
        CancellationToken cancellationToken = default);
}
