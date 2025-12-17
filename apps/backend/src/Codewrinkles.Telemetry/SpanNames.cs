namespace Codewrinkles.Telemetry;

/// <summary>
/// Constants for span names used throughout the application.
/// Using constants ensures consistent naming and makes refactoring easier.
/// </summary>
public static class SpanNames
{
    public static class Authentication
    {
        public const string Login = "Authentication.Login";
        public const string Register = "Authentication.Register";
        public const string TokenRefresh = "Authentication.TokenRefresh";
        public const string ChangePassword = "Authentication.ChangePassword";
        public const string OAuthInitiate = "Authentication.OAuth.Initiate";
        public const string OAuthCallback = "Authentication.OAuth.Callback";
        public const string OAuthTokenExchange = "Authentication.OAuth.TokenExchange";
    }

    public static class Pulse
    {
        public const string Create = "Pulse.Create";
        public const string Edit = "Pulse.Edit";
        public const string Delete = "Pulse.Delete";
        public const string Get = "Pulse.Get";
        public const string GetByAuthor = "Pulse.GetByAuthor";
        public const string GetByHashtag = "Pulse.GetByHashtag";
        public const string Reply = "Pulse.Reply";
        public const string Repulse = "Pulse.Repulse";
    }

    public static class Feed
    {
        public const string Load = "Feed.Load";
        public const string Thread = "Feed.Thread";
        public const string TrendingHashtags = "Feed.TrendingHashtags";
    }

    public static class Engagement
    {
        public const string Like = "Engagement.Like";
        public const string Unlike = "Engagement.Unlike";
        public const string Bookmark = "Engagement.Bookmark";
        public const string Unbookmark = "Engagement.Unbookmark";
        public const string GetBookmarks = "Engagement.GetBookmarks";
    }

    public static class Social
    {
        public const string Follow = "Social.Follow";
        public const string Unfollow = "Social.Unfollow";
        public const string GetFollowers = "Social.GetFollowers";
        public const string GetFollowing = "Social.GetFollowing";
        public const string IsFollowing = "Social.IsFollowing";
        public const string GetSuggestions = "Social.GetSuggestions";
        public const string GetPopular = "Social.GetPopular";
    }

    public static class Profile
    {
        public const string Get = "Profile.Get";
        public const string GetByHandle = "Profile.GetByHandle";
        public const string Update = "Profile.Update";
        public const string UploadAvatar = "Profile.UploadAvatar";
        public const string Search = "Profile.Search";
        public const string GetOnboardingStatus = "Profile.GetOnboardingStatus";
        public const string CompleteOnboarding = "Profile.CompleteOnboarding";
    }

    public static class Notification
    {
        public const string GetAll = "Notification.GetAll";
        public const string GetUnreadCount = "Notification.GetUnreadCount";
        public const string MarkAsRead = "Notification.MarkAsRead";
        public const string MarkAllAsRead = "Notification.MarkAllAsRead";
        public const string Delete = "Notification.Delete";
        public const string DeleteAll = "Notification.DeleteAll";
    }

    public static class Admin
    {
        public const string GetDashboardMetrics = "Admin.GetDashboardMetrics";
    }

    public static class External
    {
        public const string LinkPreviewFetch = "External.LinkPreview.Fetch";
    }

    public static class Validation
    {
        public const string Validate = "Validation.Validate";
    }

    public static class Nova
    {
        public const string SendMessage = "Nova.SendMessage";
        public const string GetConversations = "Nova.GetConversations";
        public const string GetConversation = "Nova.GetConversation";
        public const string DeleteConversation = "Nova.DeleteConversation";
        public const string GetLearnerProfile = "Nova.GetLearnerProfile";
        public const string UpdateLearnerProfile = "Nova.UpdateLearnerProfile";
        public const string ExtractMemories = "Nova.ExtractMemories";
        public const string GetMemoriesForContext = "Nova.GetMemoriesForContext";
        public const string TriggerMemoryExtraction = "Nova.TriggerMemoryExtraction";
        public const string ApplyForAlpha = "Nova.ApplyForAlpha";
        public const string RedeemAlphaCode = "Nova.RedeemAlphaCode";
    }
}
