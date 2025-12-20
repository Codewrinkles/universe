namespace Codewrinkles.Application.Common.Interfaces;

/// <summary>
/// Repository for Nova metrics calculations.
/// Read-only metrics that span multiple entities.
/// </summary>
public interface INovaMetricsRepository
{
    /// <summary>
    /// Get all Alpha metrics in a single repository call.
    /// </summary>
    Task<NovaAlphaMetricsData> GetAlphaMetricsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Data transfer object for Alpha metrics.
/// </summary>
public sealed record NovaAlphaMetricsData(
    // Application funnel
    int TotalApplications,
    int PendingApplications,
    int AcceptedApplications,
    int WaitlistedApplications,
    int RedeemedCodes,

    // User metrics
    int NovaUsers,
    int ActivatedUsers,
    decimal ActivationRate,

    // Engagement metrics
    int ActiveLast7Days,
    decimal ActiveRate,

    // Usage metrics
    int TotalSessions,
    int TotalMessages,
    decimal AvgSessionsPerUser,
    decimal AvgMessagesPerSession
);
