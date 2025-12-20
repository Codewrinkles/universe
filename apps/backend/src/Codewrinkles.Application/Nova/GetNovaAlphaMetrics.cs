using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

/// <summary>
/// Query to retrieve Nova Alpha metrics for the admin dashboard.
/// </summary>
public sealed record GetNovaAlphaMetricsQuery : IQuery<GetNovaAlphaMetricsResult>;

/// <summary>
/// Nova Alpha metrics result containing application funnel and user engagement data.
/// </summary>
public sealed record GetNovaAlphaMetricsResult(
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
/// Handler for GetNovaAlphaMetricsQuery.
/// </summary>
public sealed class GetNovaAlphaMetricsQueryHandler
    : IQueryHandler<GetNovaAlphaMetricsQuery, GetNovaAlphaMetricsResult>
{
    private readonly INovaMetricsRepository _metricsRepository;

    public GetNovaAlphaMetricsQueryHandler(INovaMetricsRepository metricsRepository)
    {
        _metricsRepository = metricsRepository;
    }

    public async Task<GetNovaAlphaMetricsResult> HandleAsync(
        GetNovaAlphaMetricsQuery query,
        CancellationToken cancellationToken = default)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(
            SpanNames.Admin.GetNovaAlphaMetrics);

        try
        {
            var metrics = await _metricsRepository.GetAlphaMetricsAsync(cancellationToken);

            activity?.SetSuccess(true);

            return new GetNovaAlphaMetricsResult(
                TotalApplications: metrics.TotalApplications,
                PendingApplications: metrics.PendingApplications,
                AcceptedApplications: metrics.AcceptedApplications,
                WaitlistedApplications: metrics.WaitlistedApplications,
                RedeemedCodes: metrics.RedeemedCodes,
                NovaUsers: metrics.NovaUsers,
                ActivatedUsers: metrics.ActivatedUsers,
                ActivationRate: metrics.ActivationRate,
                ActiveLast7Days: metrics.ActiveLast7Days,
                ActiveRate: metrics.ActiveRate,
                TotalSessions: metrics.TotalSessions,
                TotalMessages: metrics.TotalMessages,
                AvgSessionsPerUser: metrics.AvgSessionsPerUser,
                AvgMessagesPerSession: metrics.AvgMessagesPerSession
            );
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}
