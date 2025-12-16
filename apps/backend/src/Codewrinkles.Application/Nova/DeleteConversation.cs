using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova.Exceptions;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

public sealed record DeleteConversationCommand(
    Guid ProfileId,
    Guid SessionId
) : ICommand<DeleteConversationResult>;

public sealed record DeleteConversationResult(
    bool Success
);

public sealed class DeleteConversationCommandHandler
    : ICommandHandler<DeleteConversationCommand, DeleteConversationResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteConversationCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteConversationResult> HandleAsync(
        DeleteConversationCommand command,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Nova.DeleteConversation);
        activity?.SetProfileId(command.ProfileId);
        activity?.SetEntity("ConversationSession", command.SessionId);

        try
        {
            var session = await _unitOfWork.Nova.FindSessionByIdAsync(
                command.SessionId,
                cancellationToken);

            if (session is null)
            {
                throw new ConversationNotFoundException(command.SessionId);
            }

            if (session.ProfileId != command.ProfileId)
            {
                throw new ConversationAccessDeniedException(command.SessionId, command.ProfileId);
            }

            // Already deleted - idempotent
            if (session.IsDeleted)
            {
                return new DeleteConversationResult(Success: true);
            }

            // Soft delete
            session.MarkAsDeleted();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            activity?.SetSuccess(true);

            return new DeleteConversationResult(Success: true);
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}
