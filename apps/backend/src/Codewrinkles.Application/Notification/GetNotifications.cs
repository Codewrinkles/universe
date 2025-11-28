using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Notification;

public sealed record GetNotificationsQuery(
    Guid RecipientId,
    int Offset = 0,
    int Limit = 20
) : ICommand<GetNotificationsResult>;

public sealed record GetNotificationsResult(
    List<NotificationDto> Notifications,
    int TotalCount,
    int UnreadCount
);

public sealed record NotificationDto(
    Guid Id,
    NotificationActorDto Actor,
    string Type,
    Guid? EntityId,
    bool IsRead,
    DateTime CreatedAt
);

public sealed record NotificationActorDto(
    Guid Id,
    string Name,
    string Handle,
    string? AvatarUrl
);

public sealed class GetNotificationsQueryHandler
    : ICommandHandler<GetNotificationsQuery, GetNotificationsResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetNotificationsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetNotificationsResult> HandleAsync(
        GetNotificationsQuery query,
        CancellationToken cancellationToken)
    {
        // Get paginated notifications with actor information
        var (notifications, totalCount) = await _unitOfWork.Notifications.GetByRecipientAsync(
            query.RecipientId,
            query.Offset,
            query.Limit,
            cancellationToken);

        // Get unread count
        var unreadCount = await _unitOfWork.Notifications.GetUnreadCountAsync(
            query.RecipientId,
            cancellationToken);

        // Map to DTOs
        var notificationDtos = notifications.Select(n => new NotificationDto(
            Id: n.Id,
            Actor: new NotificationActorDto(
                Id: n.Actor.Id,
                Name: n.Actor.Name,
                Handle: n.Actor.Handle ?? "unknown",
                AvatarUrl: n.Actor.AvatarUrl
            ),
            Type: n.Type.ToString().ToLowerInvariant(), // "pulselike", "pulsereply", etc.
            EntityId: n.EntityId,
            IsRead: n.IsRead,
            CreatedAt: n.CreatedAt
        )).ToList();

        return new GetNotificationsResult(
            Notifications: notificationDtos,
            TotalCount: totalCount,
            UnreadCount: unreadCount
        );
    }
}
