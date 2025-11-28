using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse.Exceptions;

namespace Codewrinkles.Application.Pulse;

public sealed record GetPulseQuery(
    Guid PulseId,
    Guid? CurrentUserId
) : ICommand<PulseDto>;

public sealed class GetPulseQueryHandler : ICommandHandler<GetPulseQuery, PulseDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPulseQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PulseDto> HandleAsync(
        GetPulseQuery query,
        CancellationToken cancellationToken)
    {
        var pulse = await _unitOfWork.Pulses.FindByIdWithDetailsAsync(
            query.PulseId,
            cancellationToken);

        if (pulse is null)
        {
            throw new PulseNotFoundException(query.PulseId);
        }

        // Check if current user has liked this pulse
        var isLikedByCurrentUser = false;
        if (query.CurrentUserId.HasValue)
        {
            isLikedByCurrentUser = await _unitOfWork.Pulses.HasUserLikedPulseAsync(
                query.PulseId,
                query.CurrentUserId.Value,
                cancellationToken);
        }

        return MapToPulseDto(pulse, isLikedByCurrentUser);
    }

    private static PulseDto MapToPulseDto(Domain.Pulse.Pulse pulse, bool isLikedByCurrentUser)
    {
        return new PulseDto(
            Id: pulse.Id,
            Author: new PulseAuthorDto(
                Id: pulse.Author.Id,
                Name: pulse.Author.Name,
                Handle: pulse.Author.Handle ?? string.Empty,
                AvatarUrl: pulse.Author.AvatarUrl
            ),
            Content: pulse.Content,
            Type: pulse.Type.ToString().ToLowerInvariant(),
            CreatedAt: pulse.CreatedAt,
            Engagement: new PulseEngagementDto(
                ReplyCount: pulse.Engagement.ReplyCount,
                RepulseCount: pulse.Engagement.RepulseCount,
                LikeCount: pulse.Engagement.LikeCount,
                ViewCount: pulse.Engagement.ViewCount
            ),
            IsLikedByCurrentUser: isLikedByCurrentUser,
            RepulsedPulse: pulse.RepulsedPulse is not null
                ? MapToRepulsedPulseDto(pulse.RepulsedPulse)
                : null
        );
    }

    private static RepulsedPulseDto MapToRepulsedPulseDto(Domain.Pulse.Pulse pulse)
    {
        return new RepulsedPulseDto(
            Id: pulse.Id,
            Author: new PulseAuthorDto(
                Id: pulse.Author.Id,
                Name: pulse.Author.Name,
                Handle: pulse.Author.Handle ?? string.Empty,
                AvatarUrl: pulse.Author.AvatarUrl
            ),
            Content: pulse.Content,
            CreatedAt: pulse.CreatedAt,
            IsDeleted: pulse.IsDeleted
        );
    }
}
