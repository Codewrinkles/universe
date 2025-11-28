using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Notification;

public sealed record GetUnreadCountQuery(
    Guid RecipientId
) : ICommand<int>;

public sealed class GetUnreadCountQueryHandler : ICommandHandler<GetUnreadCountQuery, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUnreadCountQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> HandleAsync(
        GetUnreadCountQuery query,
        CancellationToken cancellationToken)
    {
        // Direct call to repository - no unnecessary queries or mapping
        return await _unitOfWork.Notifications.GetUnreadCountAsync(
            query.RecipientId,
            cancellationToken);
    }
}
