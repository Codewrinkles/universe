namespace Codewrinkles.Telemetry;

/// <summary>
/// Constants for tag/attribute names used on spans throughout the application.
/// Using constants ensures consistent naming across all telemetry.
/// </summary>
public static class TagNames
{
    /// <summary>
    /// User identity attributes.
    /// </summary>
    public static class User
    {
        /// <summary>
        /// The IdentityId - authentication/account identifier.
        /// </summary>
        public const string IdentityId = "user.identity_id";

        /// <summary>
        /// The ProfileId - social/public identifier.
        /// </summary>
        public const string ProfileId = "user.profile_id";

        /// <summary>
        /// Email domain (e.g., "@gmail.com") - never log full email.
        /// </summary>
        public const string EmailDomain = "user.email_domain";
    }

    /// <summary>
    /// Operation attributes.
    /// </summary>
    public static class Operation
    {
        public const string Name = "operation.name";
        public const string Type = "operation.type";
        public const string Success = "operation.success";
    }

    /// <summary>
    /// Entity attributes.
    /// </summary>
    public static class Entity
    {
        public const string Type = "entity.type";
        public const string Id = "entity.id";
    }

    /// <summary>
    /// Database/query attributes.
    /// </summary>
    public static class Database
    {
        public const string RecordCount = "db.record_count";
        public const string QueryType = "db.query_type";
    }

    /// <summary>
    /// Authentication attributes.
    /// </summary>
    public static class Auth
    {
        public const string Method = "auth.method";
        public const string Provider = "auth.provider";
        public const string TokenExpired = "auth.token_expired";
        public const string FailureReason = "auth.failure_reason";
    }

    /// <summary>
    /// Pulse-specific attributes.
    /// </summary>
    public static class Pulse
    {
        public const string Id = "pulse.id";
        public const string Type = "pulse.type";
        public const string HasImage = "pulse.has_image";
        public const string HasLink = "pulse.has_link";
        public const string MentionCount = "pulse.mention_count";
        public const string HashtagCount = "pulse.hashtag_count";
        public const string ParentPulseId = "pulse.parent_pulse_id";
    }

    /// <summary>
    /// Social attributes.
    /// </summary>
    public static class Social
    {
        public const string TargetProfileId = "social.target_profile_id";
        public const string FollowerCount = "social.follower_count";
        public const string FollowingCount = "social.following_count";
    }

    /// <summary>
    /// Feed attributes.
    /// </summary>
    public static class Feed
    {
        public const string CursorType = "feed.cursor_type";
        public const string ResultCount = "feed.result_count";
        public const string HasMoreResults = "feed.has_more_results";
    }

    /// <summary>
    /// External service attributes.
    /// </summary>
    public static class External
    {
        public const string ServiceName = "external.service_name";
        public const string Domain = "external.domain";
    }

    /// <summary>
    /// Error attributes.
    /// </summary>
    public static class Error
    {
        public const string Type = "error.type";
        public const string Message = "error.message";
    }

    /// <summary>
    /// Validation attributes.
    /// </summary>
    public static class Validation
    {
        public const string ValidatorName = "validation.validator_name";
        public const string IsValid = "validation.is_valid";
        public const string ErrorCount = "validation.error_count";
    }
}
