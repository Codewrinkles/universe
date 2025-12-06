using Codewrinkles.Application.Common.Exceptions;
using Codewrinkles.Application.Common.Interfaces;
using Kommand;

namespace Codewrinkles.Application.Notification;

public sealed class DeleteNotificationValidator : IValidator<DeleteNotificationCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteNotificationValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ValidationResult> ValidateAsync(
        DeleteNotificationCommand request,
        CancellationToken cancellationToken)
    {
        // Business rule validation - ensure notification exists and user is the recipient
        var notification = await _unitOfWork.Notifications.FindByIdAsync(request.NotificationId, cancellationToken);

        if (notification is null)
        {
            throw new NotificationNotFoundException(request.NotificationId);
        }

        // Verify user is the recipient
        if (notification.RecipientId != request.UserId)
        {
            throw new UnauthorizedNotificationAccessException(request.NotificationId, request.UserId);
        }

        return ValidationResult.Success();
    }
}
