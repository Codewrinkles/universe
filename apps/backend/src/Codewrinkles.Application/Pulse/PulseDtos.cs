namespace Codewrinkles.Application.Pulse;

public sealed record PulseDto(
    Guid Id,
    PulseAuthorDto Author,
    string Content,
    string Type,
    DateTime CreatedAt,
    PulseEngagementDto Engagement,
    bool IsLikedByCurrentUser,
    bool IsFollowingAuthor,
    bool IsBookmarkedByCurrentUser,
    Guid? ParentPulseId,
    Guid? ThreadRootId,
    ReplyingToDto? ReplyingTo,
    RepulsedPulseDto? RepulsedPulse,
    string? ImageUrl,
    PulseLinkPreviewDto? LinkPreview,
    List<MentionDto> Mentions
);

/// <summary>
/// Context for nested replies - shows who the reply is responding to.
/// Only populated when replying to another reply (not the thread root).
/// </summary>
public sealed record ReplyingToDto(
    Guid PulseId,
    string AuthorHandle,
    string AuthorName
);

public sealed record MentionDto(
    Guid ProfileId,
    string Handle
);

public sealed record PulseAuthorDto(
    Guid Id,
    string Name,
    string Handle,
    string? AvatarUrl
);

public sealed record PulseEngagementDto(
    int ReplyCount,
    int RepulseCount,
    int LikeCount,
    long ViewCount
);

public sealed record RepulsedPulseDto(
    Guid Id,
    PulseAuthorDto Author,
    string Content,
    DateTime CreatedAt,
    bool IsDeleted
);

public sealed record PulseLinkPreviewDto(
    string Url,
    string Title,
    string? Description,
    string? ImageUrl,
    string Domain
);

public sealed record FeedResponse(
    IReadOnlyList<PulseDto> Pulses,
    string? NextCursor,
    bool HasMore
);

public sealed record ThreadResponse(
    PulseDto ParentPulse,
    IReadOnlyList<PulseDto> Replies,
    int TotalReplyCount,
    string? NextCursor,
    bool HasMore
);
