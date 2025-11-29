using Codewrinkles.Application.Admin;
using Kommand.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Codewrinkles.API.Modules.Admin;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/dashboard/metrics", GetDashboardMetrics)
            .WithName("GetDashboardMetrics");
    }

    private static async Task<IResult> GetDashboardMetrics(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetDashboardMetricsQuery();
        var result = await mediator.SendAsync(query, cancellationToken);

        return Results.Ok(new
        {
            totalUsers = result.TotalUsers,
            activeUsers = result.ActiveUsers,
            totalPulses = result.TotalPulses
        });
    }
}
