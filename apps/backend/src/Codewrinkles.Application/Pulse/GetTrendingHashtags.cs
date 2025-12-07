using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Pulse;

public sealed record GetTrendingHashtagsQuery(
    int Limit = 10
) : ICommand<List<HashtagDto>>;

public sealed record HashtagDto(
    Guid Id,
    string Tag,
    string TagDisplay,
    int PulseCount,
    DateTimeOffset LastUsedAt
);

public sealed class GetTrendingHashtagsQueryHandler
    : ICommandHandler<GetTrendingHashtagsQuery, List<HashtagDto>>
{
    private readonly IHashtagRepository _hashtagRepository;

    public GetTrendingHashtagsQueryHandler(IHashtagRepository hashtagRepository)
    {
        _hashtagRepository = hashtagRepository;
    }

    public async Task<List<HashtagDto>> HandleAsync(
        GetTrendingHashtagsQuery query,
        CancellationToken cancellationToken)
    {
        var hashtags = await _hashtagRepository.GetTrendingHashtagsAsync(
            query.Limit,
            cancellationToken);

        return hashtags
            .Select(h => new HashtagDto(
                Id: h.Id,
                Tag: h.Tag,
                TagDisplay: h.TagDisplay,
                PulseCount: h.PulseCount,
                LastUsedAt: h.LastUsedAt))
            .ToList();
    }
}
