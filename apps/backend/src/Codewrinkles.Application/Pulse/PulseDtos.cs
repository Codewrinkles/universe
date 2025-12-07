namespace Codewrinkles.Application.Pulse;

public sealed record PulseDto(
    Guid Id,
    PulseAuthorDto Author,
    string Content,
    string Type,
    DateTimeOffset CreatedAt,
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
/// Context for all replies - shows who the reply is responding to.
/// Populated for both direct replies (to thread root) and nested replies (to other replies).
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
    DateTimeOffset CreatedAt,
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
    bool HasMore,
    int? TotalCount = null
);

public sealed record ThreadResponse(
    PulseDto ParentPulse,
    IReadOnlyList<PulseDto> Replies,
    int TotalReplyCount,
    string? NextCursor,
    bool HasMore
);
