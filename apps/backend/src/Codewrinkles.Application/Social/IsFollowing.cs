using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Social;

public sealed record IsFollowingQuery(
    Guid FollowerId,
    Guid FollowingId
) : ICommand<IsFollowingResult>;

public sealed record IsFollowingResult(bool IsFollowing);

public sealed class IsFollowingQueryHandler : ICommandHandler<IsFollowingQuery, IsFollowingResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public IsFollowingQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IsFollowingResult> HandleAsync(
        IsFollowingQuery query,
        CancellationToken cancellationToken)
    {
        var isFollowing = await _unitOfWork.Follows.IsFollowingAsync(
            query.FollowerId,
            query.FollowingId,
            cancellationToken);

        return new IsFollowingResult(IsFollowing: isFollowing);
    }
}
