using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Social;

public sealed record GetPopularProfilesQuery(int Limit = 10, Guid? ExcludeProfileId = null) : ICommand<PopularProfilesResponse>;

public sealed record PopularProfilesResponse(IReadOnlyList<ProfileSuggestion> Profiles);

public sealed class GetPopularProfilesQueryHandler
    : ICommandHandler<GetPopularProfilesQuery, PopularProfilesResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPopularProfilesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PopularProfilesResponse> HandleAsync(
        GetPopularProfilesQuery query,
        CancellationToken cancellationToken)
    {
        var popular = await _unitOfWork.Profiles.GetMostFollowedProfilesAsync(
            query.Limit,
            query.ExcludeProfileId,
            cancellationToken);

        var dtos = popular.Select(p => new ProfileSuggestion(
            ProfileId: p.Id,
            Name: p.Name,
            Handle: p.Handle ?? string.Empty,
            AvatarUrl: p.AvatarUrl,
            Bio: p.Bio,
            MutualFollowCount: 0 // Not applicable for popular profiles
        )).ToList();

        return new PopularProfilesResponse(dtos);
    }
}
