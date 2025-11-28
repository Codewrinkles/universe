using Codewrinkles.Application.Notification;
using Codewrinkles.API.Extensions;
using Kommand.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Codewrinkles.API.Modules.Notification;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/notifications")
            .WithTags("Notifications");

        group.MapGet("", GetNotifications)
            .WithName("GetNotifications")
            .RequireAuthorization();

        group.MapGet("unread-count", GetUnreadCount)
            .WithName("GetUnreadCount")
            .RequireAuthorization();

        group.MapPut("{id:guid}/read", MarkAsRead)
            .WithName("MarkNotificationAsRead")
            .RequireAuthorization();

        group.MapPut("read-all", MarkAllAsRead)
            .WithName("MarkAllNotificationsAsRead")
            .RequireAuthorization();

        return app;
    }

    private static async Task<IResult> GetNotifications(
        HttpContext httpContext,
        [FromServices] IMediator mediator,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract ProfileId from JWT claims
            var profileId = httpContext.GetCurrentProfileId();

            // Validate pagination parameters
            if (limit < 1 || limit > 100)
            {
                return Results.Problem(
                    title: "Invalid Limit",
                    detail: "Limit must be between 1 and 100",
                    statusCode: 400
                );
            }

            if (offset < 0)
            {
                return Results.Problem(
                    title: "Invalid Offset",
                    detail: "Offset must be >= 0",
                    statusCode: 400
                );
            }

            var query = new GetNotificationsQuery(
                RecipientId: profileId,
                Offset: offset,
                Limit: limit
            );

            var result = await mediator.SendAsync(query, cancellationToken);

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Failed to Retrieve Notifications",
                detail: ex.Message,
                statusCode: 500
            );
        }
    }

    private static async Task<IResult> GetUnreadCount(
        HttpContext httpContext,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract ProfileId from JWT claims
            var profileId = httpContext.GetCurrentProfileId();

            // Use the GetNotifications query with limit 0 to just get unread count
            var query = new GetNotificationsQuery(
                RecipientId: profileId,
                Offset: 0,
                Limit: 0
            );

            var result = await mediator.SendAsync(query, cancellationToken);

            return Results.Ok(new { unreadCount = result.UnreadCount });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Failed to Retrieve Unread Count",
                detail: ex.Message,
                statusCode: 500
            );
        }
    }

    private static async Task<IResult> MarkAsRead(
        HttpContext httpContext,
        Guid id,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract ProfileId from JWT claims
            var profileId = httpContext.GetCurrentProfileId();

            var command = new MarkNotificationAsReadCommand(
                NotificationId: id,
                UserId: profileId
            );

            var result = await mediator.SendAsync(command, cancellationToken);

            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Invalid Operation",
                detail: ex.Message,
                statusCode: 400
            );
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Failed to Mark Notification as Read",
                detail: ex.Message,
                statusCode: 500
            );
        }
    }

    private static async Task<IResult> MarkAllAsRead(
        HttpContext httpContext,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract ProfileId from JWT claims
            var profileId = httpContext.GetCurrentProfileId();

            var command = new MarkAllNotificationsAsReadCommand(
                UserId: profileId
            );

            var result = await mediator.SendAsync(command, cancellationToken);

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Failed to Mark All Notifications as Read",
                detail: ex.Message,
                statusCode: 500
            );
        }
    }
}
