using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Users;

// Query
public sealed record SearchProfilesQuery(
    string Query,
    int Limit = 20
) : ICommand<SearchProfilesResult>;

// Result
public sealed record SearchProfilesResult(
    IReadOnlyList<ProfileSearchResultDto> Profiles
);

public sealed record ProfileSearchResultDto(
    Guid ProfileId,
    string Name,
    string? Handle,
    string? Bio,
    string? AvatarUrl
);

// Handler
public sealed class SearchProfilesQueryHandler : ICommandHandler<SearchProfilesQuery, SearchProfilesResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public SearchProfilesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SearchProfilesResult> HandleAsync(
        SearchProfilesQuery query,
        CancellationToken cancellationToken)
    {
        var profiles = await _unitOfWork.Profiles.SearchProfilesAsync(
            query.Query,
            query.Limit,
            cancellationToken);

        var results = profiles.Select(p => new ProfileSearchResultDto(
            ProfileId: p.Id,
            Name: p.Name,
            Handle: p.Handle,
            Bio: p.Bio,
            AvatarUrl: p.AvatarUrl
        )).ToList();

        return new SearchProfilesResult(results);
    }
}
