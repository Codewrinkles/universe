using Codewrinkles.Application.Admin;
using Codewrinkles.Application.Nova;
using Codewrinkles.Domain.Nova;
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

        // Alpha application management
        group.MapGet("/alpha/applications", GetAlphaApplications)
            .WithName("GetAlphaApplications");

        group.MapPost("/alpha/applications/{id:guid}/accept", AcceptAlphaApplication)
            .WithName("AcceptAlphaApplication");

        group.MapPost("/alpha/applications/{id:guid}/waitlist", WaitlistAlphaApplication)
            .WithName("WaitlistAlphaApplication");

        // User management
        group.MapGet("/users", GetAdminUsers)
            .WithName("GetAdminUsers");
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

    private static async Task<IResult> GetAlphaApplications(
        [FromServices] IMediator mediator,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        AlphaApplicationStatus? statusFilter = status?.ToLowerInvariant() switch
        {
            "pending" => AlphaApplicationStatus.Pending,
            "accepted" => AlphaApplicationStatus.Accepted,
            "waitlisted" => AlphaApplicationStatus.Waitlisted,
            _ => null
        };

        var query = new GetAlphaApplicationsQuery(statusFilter);
        var result = await mediator.SendAsync(query, cancellationToken);

        return Results.Ok(new
        {
            applications = result.Applications.Select(a => new
            {
                id = a.Id,
                email = a.Email,
                name = a.Name,
                primaryTechStack = a.PrimaryTechStack,
                yearsOfExperience = a.YearsOfExperience,
                goal = a.Goal,
                status = a.Status.ToString().ToLowerInvariant(),
                inviteCode = a.InviteCode,
                inviteCodeRedeemed = a.InviteCodeRedeemed,
                createdAt = a.CreatedAt
            })
        });
    }

    private static async Task<IResult> AcceptAlphaApplication(
        [FromServices] IMediator mediator,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new AcceptAlphaApplicationCommand(id);
            var result = await mediator.SendAsync(command, cancellationToken);

            return Results.Ok(new
            {
                inviteCode = result.InviteCode,
                message = "Application accepted. Invite code email sent."
            });
        }
        catch (AlphaApplicationNotFoundException)
        {
            return Results.NotFound(new { message = "Application not found" });
        }
        catch (AlphaApplicationNotPendingException)
        {
            return Results.BadRequest(new { message = "Application is not in pending status" });
        }
    }

    private static async Task<IResult> WaitlistAlphaApplication(
        [FromServices] IMediator mediator,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new WaitlistAlphaApplicationCommand(id);
            await mediator.SendAsync(command, cancellationToken);

            return Results.Ok(new
            {
                message = "Application waitlisted. Notification email sent."
            });
        }
        catch (AlphaApplicationNotFoundException)
        {
            return Results.NotFound(new { message = "Application not found" });
        }
        catch (AlphaApplicationNotPendingException)
        {
            return Results.BadRequest(new { message = "Application is not in pending status" });
        }
    }

    private static async Task<IResult> GetAdminUsers(
        [FromServices] IMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Clamp page size to reasonable limits
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var query = new GetAdminUsersQuery(page, pageSize);
        var result = await mediator.SendAsync(query, cancellationToken);

        return Results.Ok(new
        {
            users = result.Users.Select(u => new
            {
                profileId = u.ProfileId,
                name = u.Name,
                handle = u.Handle,
                avatarUrl = u.AvatarUrl,
                email = u.Email,
                createdAt = u.CreatedAt
            }),
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
            totalPages = result.TotalPages
        });
    }
}
