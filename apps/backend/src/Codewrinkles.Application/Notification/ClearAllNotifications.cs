using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Telemetry;

namespace Codewrinkles.Application.Notification;

public sealed record ClearAllNotificationsCommand(
    Guid UserId
) : ICommand<ClearAllNotificationsResult>;

public sealed record ClearAllNotificationsResult(
    int DeletedCount
);

public sealed class ClearAllNotificationsCommandHandler
    : ICommandHandler<ClearAllNotificationsCommand, ClearAllNotificationsResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public ClearAllNotificationsCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ClearAllNotificationsResult> HandleAsync(
        ClearAllNotificationsCommand command,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Notification.DeleteAll);
        activity?.SetProfileId(command.UserId);

        try
        {
            // Delete all notifications for this user
            var deletedCount = await _unitOfWork.Notifications.DeleteAllForRecipientAsync(
                command.UserId,
                cancellationToken);

            activity?.SetTag(TagNames.Database.RecordCount, deletedCount);
            activity?.SetSuccess(true);

            return new ClearAllNotificationsResult(DeletedCount: deletedCount);
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}
