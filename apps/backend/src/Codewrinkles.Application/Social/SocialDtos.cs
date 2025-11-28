namespace Codewrinkles.Application.Social;

public sealed record FollowResult(bool Success);

public sealed record UnfollowResult(bool Success);

public sealed record FollowerDto(
    Guid ProfileId,
    string Name,
    string Handle,
    string? AvatarUrl,
    string? Bio,
    DateTime FollowedAt
);

public sealed record FollowingDto(
    Guid ProfileId,
    string Name,
    string Handle,
    string? AvatarUrl,
    string? Bio,
    DateTime FollowedAt
);

public sealed record ProfileSuggestion(
    Guid ProfileId,
    string Name,
    string Handle,
    string? AvatarUrl,
    string? Bio,
    int MutualFollowCount
);

public sealed record FollowersResponse(
    IReadOnlyList<FollowerDto> Followers,
    int TotalCount,
    string? NextCursor,
    bool HasMore
);

public sealed record FollowingResponse(
    IReadOnlyList<FollowingDto> Following,
    int TotalCount,
    string? NextCursor,
    bool HasMore
);

public sealed record SuggestedProfilesResponse(
    IReadOnlyList<ProfileSuggestion> Suggestions
);
