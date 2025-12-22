using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

/// <summary>
/// Query to retrieve per-user Nova usage metrics for the admin dashboard.
/// </summary>
public sealed record GetNovaUserUsageQuery : IQuery<GetNovaUserUsageResult>;

/// <summary>
/// Per-user Nova usage metrics result.
/// </summary>
public sealed record GetNovaUserUsageResult(IReadOnlyList<UserUsageDto> Users);

/// <summary>
/// Individual user usage data for the API response.
/// </summary>
public sealed record UserUsageDto(
    Guid ProfileId,
    string Name,
    string? Handle,
    string? AvatarUrl,
    NovaAccessLevel AccessLevel,
    int SessionsLast24Hours,
    int SessionsLast3Days,
    int SessionsLast7Days,
    int SessionsLast30Days,
    int TotalMessages,
    decimal AvgMessagesPerSession,
    DateTimeOffset? LastActiveAt,
    DateTimeOffset? FirstSessionAt,
    int SessionsPrevious7Days,
    decimal TrendPercentage
);

/// <summary>
/// Handler for GetNovaUserUsageQuery.
/// </summary>
public sealed class GetNovaUserUsageQueryHandler
    : IQueryHandler<GetNovaUserUsageQuery, GetNovaUserUsageResult>
{
    private readonly INovaMetricsRepository _metricsRepository;

    public GetNovaUserUsageQueryHandler(INovaMetricsRepository metricsRepository)
    {
        _metricsRepository = metricsRepository;
    }

    public async Task<GetNovaUserUsageResult> HandleAsync(
        GetNovaUserUsageQuery query,
        CancellationToken cancellationToken = default)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(
            SpanNames.Admin.GetNovaUserUsage);

        try
        {
            var usageData = await _metricsRepository.GetUserUsageAsync(cancellationToken);

            var users = usageData.Select(u => new UserUsageDto(
                ProfileId: u.ProfileId,
                Name: u.Name,
                Handle: u.Handle,
                AvatarUrl: u.AvatarUrl,
                AccessLevel: u.AccessLevel,
                SessionsLast24Hours: u.SessionsLast24Hours,
                SessionsLast3Days: u.SessionsLast3Days,
                SessionsLast7Days: u.SessionsLast7Days,
                SessionsLast30Days: u.SessionsLast30Days,
                TotalMessages: u.TotalMessages,
                AvgMessagesPerSession: u.AvgMessagesPerSession,
                LastActiveAt: u.LastActiveAt,
                FirstSessionAt: u.FirstSessionAt,
                SessionsPrevious7Days: u.SessionsPrevious7Days,
                TrendPercentage: u.TrendPercentage
            )).ToList();

            activity?.SetSuccess(true);

            return new GetNovaUserUsageResult(users);
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}
