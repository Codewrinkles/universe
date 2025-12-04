using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Telemetry;

namespace Codewrinkles.Application.Admin;

public sealed record GetDashboardMetricsQuery : ICommand<DashboardMetricsResponse>;

public sealed record DashboardMetricsResponse(
    int TotalUsers,
    int ActiveUsers,
    int TotalPulses);

public sealed class GetDashboardMetricsQueryHandler
    : ICommandHandler<GetDashboardMetricsQuery, DashboardMetricsResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetDashboardMetricsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DashboardMetricsResponse> HandleAsync(
        GetDashboardMetricsQuery query,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Admin.GetDashboardMetrics);

        try
        {
            // Execute count queries sequentially (DbContext is not thread-safe for concurrent operations)
            var totalUsers = await _unitOfWork.Identities.GetTotalCountAsync(cancellationToken);

            var activeUsersSince = DateTime.UtcNow.AddDays(-30);
            var activeUsers = await _unitOfWork.Identities.GetActiveCountSinceAsync(activeUsersSince, cancellationToken);

            var totalPulses = await _unitOfWork.Pulses.GetTotalCountAsync(cancellationToken);

            activity?.SetTag("admin.total_users", totalUsers);
            activity?.SetTag("admin.active_users", activeUsers);
            activity?.SetTag("admin.total_pulses", totalPulses);
            activity?.SetSuccess(true);

            return new DashboardMetricsResponse(
                TotalUsers: totalUsers,
                ActiveUsers: activeUsers,
                TotalPulses: totalPulses);
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}
