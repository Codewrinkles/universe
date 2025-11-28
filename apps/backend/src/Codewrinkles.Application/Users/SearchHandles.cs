using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Users;

// Query
public sealed record SearchHandlesQuery(string SearchTerm, int Limit = 10) : ICommand<SearchHandlesResult>;

// Result
public sealed record SearchHandlesResult(List<HandleSearchDto> Handles);

public sealed record HandleSearchDto(
    Guid ProfileId,
    string Handle,
    string Name,
    string? AvatarUrl
);

// Handler
public sealed class SearchHandlesQueryHandler : ICommandHandler<SearchHandlesQuery, SearchHandlesResult>
{
    private readonly IProfileRepository _profileRepository;

    public SearchHandlesQueryHandler(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<SearchHandlesResult> HandleAsync(SearchHandlesQuery request, CancellationToken cancellationToken)
    {
        // Search handles that start with the search term (case-insensitive)
        var profiles = await _profileRepository.SearchByHandleAsync(
            request.SearchTerm,
            request.Limit,
            cancellationToken);

        var handles = profiles
            .Select(p => new HandleSearchDto(
                p.Id,
                p.Handle!,
                p.Name,
                p.AvatarUrl))
            .ToList();

        return new SearchHandlesResult(handles);
    }
}
