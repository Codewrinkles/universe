using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Codewrinkles.Domain.Nova.Exceptions;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

public sealed record GetConversationQuery(
    Guid ProfileId,
    Guid SessionId
) : ICommand<GetConversationResult>;

public sealed record GetConversationResult(
    Guid Id,
    string? Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastMessageAt,
    IReadOnlyList<MessageDto> Messages
);

public sealed record MessageDto(
    Guid Id,
    string Role,
    string Content,
    DateTimeOffset CreatedAt
);

public sealed class GetConversationQueryHandler
    : ICommandHandler<GetConversationQuery, GetConversationResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetConversationQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetConversationResult> HandleAsync(
        GetConversationQuery query,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Nova.GetConversation);
        activity?.SetProfileId(query.ProfileId);
        activity?.SetEntity("ConversationSession", query.SessionId);

        try
        {
            var session = await _unitOfWork.Nova.FindSessionByIdWithMessagesAsync(
                query.SessionId,
                cancellationToken);

            if (session is null || session.IsDeleted)
            {
                throw new ConversationNotFoundException(query.SessionId);
            }

            if (session.ProfileId != query.ProfileId)
            {
                throw new ConversationAccessDeniedException(query.SessionId, query.ProfileId);
            }

            // Filter out system messages from the response
            var messages = session.Messages
                .Where(m => m.Role != MessageRole.System)
                .Select(m => new MessageDto(
                    Id: m.Id,
                    Role: m.Role.ToString().ToLowerInvariant(),
                    Content: m.Content,
                    CreatedAt: m.CreatedAt))
                .ToList();

            activity?.SetSuccess(true);

            return new GetConversationResult(
                Id: session.Id,
                Title: session.Title,
                CreatedAt: session.CreatedAt,
                LastMessageAt: session.LastMessageAt,
                Messages: messages);
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}
