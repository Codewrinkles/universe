using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Social;

public sealed record GetSuggestedProfilesQuery(
    Guid CurrentUserId,
    int Limit = 10
) : ICommand<SuggestedProfilesResponse>;

public sealed class GetSuggestedProfilesQueryHandler
    : ICommandHandler<GetSuggestedProfilesQuery, SuggestedProfilesResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSuggestedProfilesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SuggestedProfilesResponse> HandleAsync(
        GetSuggestedProfilesQuery query,
        CancellationToken cancellationToken)
    {
        // Get suggested profiles with mutual follow counts
        var suggestions = await _unitOfWork.Follows.GetSuggestedProfilesAsync(
            query.CurrentUserId,
            query.Limit,
            cancellationToken);

        // Map to DTOs
        var suggestionDtos = suggestions.Select(s => new ProfileSuggestion(
            ProfileId: s.Profile.Id,
            Name: s.Profile.Name,
            Handle: s.Profile.Handle ?? string.Empty,
            AvatarUrl: s.Profile.AvatarUrl,
            Bio: s.Profile.Bio,
            MutualFollowCount: s.MutualFollowCount
        )).ToList();

        return new SuggestedProfilesResponse(Suggestions: suggestionDtos);
    }
}
