namespace Codewrinkles.Telemetry;

/// <summary>
/// Constants for metric names used throughout the application.
/// Using constants ensures consistent naming and makes discovery easier.
/// </summary>
public static class MetricNames
{
    /// <summary>
    /// Business metrics - understanding user behavior and feature usage.
    /// </summary>
    public static class Business
    {
        public const string UsersRegistered = "codewrinkles.users.registered";
        public const string UsersLoggedIn = "codewrinkles.users.logged_in";
        public const string PulsesCreated = "codewrinkles.pulses.created";
        public const string PulsesDeleted = "codewrinkles.pulses.deleted";
        public const string PulseLikes = "codewrinkles.pulses.likes";
        public const string PulseReplies = "codewrinkles.pulses.replies";
        public const string PulseRepulses = "codewrinkles.pulses.repulses";
        public const string PulseBookmarks = "codewrinkles.pulses.bookmarks";
        public const string FollowsCreated = "codewrinkles.follows.created";
        public const string FollowsRemoved = "codewrinkles.follows.removed";
        public const string FeedLoads = "codewrinkles.feed.loads";
        public const string ProfileViews = "codewrinkles.profiles.views";
        public const string ProfileSearches = "codewrinkles.profiles.searches";
        public const string NotificationsCreated = "codewrinkles.notifications.created";
    }

    /// <summary>
    /// Technical metrics - infrastructure and performance.
    /// </summary>
    public static class Technical
    {
        public const string HandlerDuration = "codewrinkles.handler.duration";
        public const string ValidationDuration = "codewrinkles.validation.duration";
        public const string AuthTokenRefreshes = "codewrinkles.auth.token_refreshes";
        public const string AuthLoginAttempts = "codewrinkles.auth.login_attempts";
        public const string OAuthCallbacks = "codewrinkles.oauth.callbacks";
        public const string ImageUploads = "codewrinkles.images.uploads";
        public const string LinkPreviewFetches = "codewrinkles.link_preview.fetches";
    }
}
