using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

public sealed record GetConversationsQuery(
    Guid ProfileId,
    int Limit = 20,
    DateTimeOffset? BeforeLastMessageAt = null,
    Guid? BeforeId = null
) : ICommand<GetConversationsResult>;

public sealed record GetConversationsResult(
    IReadOnlyList<ConversationSummary> Conversations,
    bool HasMore
);

public sealed record ConversationSummary(
    Guid Id,
    string? Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastMessageAt,
    int MessageCount
);

public sealed class GetConversationsQueryHandler
    : ICommandHandler<GetConversationsQuery, GetConversationsResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetConversationsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetConversationsResult> HandleAsync(
        GetConversationsQuery query,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Nova.GetConversations);
        activity?.SetProfileId(query.ProfileId);

        try
        {
            // Fetch one more than requested to determine if there are more
            var sessions = await _unitOfWork.Nova.GetSessionsByProfileIdAsync(
                query.ProfileId,
                query.Limit + 1,
                query.BeforeLastMessageAt,
                query.BeforeId,
                cancellationToken);

            var hasMore = sessions.Count > query.Limit;
            var sessionsToReturn = hasMore ? sessions.Take(query.Limit) : sessions;

            // Get message counts for each session
            var summaries = new List<ConversationSummary>();
            foreach (var session in sessionsToReturn)
            {
                var messageCount = await _unitOfWork.Nova.GetMessageCountBySessionIdAsync(
                    session.Id,
                    cancellationToken);

                summaries.Add(new ConversationSummary(
                    Id: session.Id,
                    Title: session.Title,
                    CreatedAt: session.CreatedAt,
                    LastMessageAt: session.LastMessageAt,
                    MessageCount: messageCount));
            }

            activity?.SetSuccess(true);

            return new GetConversationsResult(
                Conversations: summaries,
                HasMore: hasMore);
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}
