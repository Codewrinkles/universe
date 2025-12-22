using Codewrinkles.Domain.Nova;

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

    /// <summary>
    /// Get per-user usage metrics for all Nova users.
    /// </summary>
    Task<IReadOnlyList<UserNovaUsageData>> GetUserUsageAsync(CancellationToken cancellationToken = default);
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

/// <summary>
/// Per-user Nova usage metrics.
/// </summary>
public sealed record UserNovaUsageData(
    // User info
    Guid ProfileId,
    string Name,
    string? Handle,
    string? AvatarUrl,
    NovaAccessLevel AccessLevel,

    // Session counts by time period
    int SessionsLast24Hours,
    int SessionsLast3Days,
    int SessionsLast7Days,
    int SessionsLast30Days,

    // Message metrics
    int TotalMessages,
    decimal AvgMessagesPerSession,

    // Engagement context
    DateTimeOffset? LastActiveAt,
    DateTimeOffset? FirstSessionAt,

    // Trend (comparing last 7d vs previous 7d)
    int SessionsPrevious7Days,
    decimal TrendPercentage
);
