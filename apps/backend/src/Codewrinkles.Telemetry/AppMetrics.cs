using System.Diagnostics.Metrics;

namespace Codewrinkles.Telemetry;

/// <summary>
/// Application metrics instruments.
/// All metric instruments are created once and reused throughout the application.
/// </summary>
public static class AppMetrics
{
    // Business metrics
    private static readonly Counter<long> s_usersRegistered = Meters.Business.CreateCounter<long>(
        MetricNames.Business.UsersRegistered,
        unit: "users",
        description: "Number of users registered");

    private static readonly Counter<long> s_usersLoggedIn = Meters.Business.CreateCounter<long>(
        MetricNames.Business.UsersLoggedIn,
        unit: "logins",
        description: "Number of successful logins");

    private static readonly Counter<long> s_pulsesCreated = Meters.Business.CreateCounter<long>(
        MetricNames.Business.PulsesCreated,
        unit: "pulses",
        description: "Number of pulses created");

    private static readonly Counter<long> s_pulsesEdited = Meters.Business.CreateCounter<long>(
        MetricNames.Business.PulsesEdited,
        unit: "pulses",
        description: "Number of pulses edited");

    private static readonly Counter<long> s_pulsesDeleted = Meters.Business.CreateCounter<long>(
        MetricNames.Business.PulsesDeleted,
        unit: "pulses",
        description: "Number of pulses deleted");

    private static readonly Counter<long> s_pulseLikes = Meters.Business.CreateCounter<long>(
        MetricNames.Business.PulseLikes,
        unit: "likes",
        description: "Number of pulse likes");

    private static readonly Counter<long> s_pulseReplies = Meters.Business.CreateCounter<long>(
        MetricNames.Business.PulseReplies,
        unit: "replies",
        description: "Number of pulse replies");

    private static readonly Counter<long> s_pulseRepulses = Meters.Business.CreateCounter<long>(
        MetricNames.Business.PulseRepulses,
        unit: "repulses",
        description: "Number of repulses");

    private static readonly Counter<long> s_pulseBookmarks = Meters.Business.CreateCounter<long>(
        MetricNames.Business.PulseBookmarks,
        unit: "bookmarks",
        description: "Number of pulse bookmarks");

    private static readonly Counter<long> s_followsCreated = Meters.Business.CreateCounter<long>(
        MetricNames.Business.FollowsCreated,
        unit: "follows",
        description: "Number of follows created");

    private static readonly Counter<long> s_followsRemoved = Meters.Business.CreateCounter<long>(
        MetricNames.Business.FollowsRemoved,
        unit: "unfollows",
        description: "Number of follows removed");

    private static readonly Counter<long> s_feedLoads = Meters.Business.CreateCounter<long>(
        MetricNames.Business.FeedLoads,
        unit: "requests",
        description: "Number of feed loads");

    private static readonly Counter<long> s_profileViews = Meters.Business.CreateCounter<long>(
        MetricNames.Business.ProfileViews,
        unit: "views",
        description: "Number of profile views");

    private static readonly Counter<long> s_profileSearches = Meters.Business.CreateCounter<long>(
        MetricNames.Business.ProfileSearches,
        unit: "searches",
        description: "Number of profile searches");

    private static readonly Counter<long> s_notificationsCreated = Meters.Business.CreateCounter<long>(
        MetricNames.Business.NotificationsCreated,
        unit: "notifications",
        description: "Number of notifications created");

    // Technical metrics
    private static readonly Histogram<double> s_handlerDuration = Meters.Technical.CreateHistogram<double>(
        MetricNames.Technical.HandlerDuration,
        unit: "ms",
        description: "Duration of handler execution");

    private static readonly Histogram<double> s_validationDuration = Meters.Technical.CreateHistogram<double>(
        MetricNames.Technical.ValidationDuration,
        unit: "ms",
        description: "Duration of validation execution");

    private static readonly Counter<long> s_authTokenRefreshes = Meters.Technical.CreateCounter<long>(
        MetricNames.Technical.AuthTokenRefreshes,
        unit: "refreshes",
        description: "Number of token refresh operations");

    private static readonly Counter<long> s_authLoginAttempts = Meters.Technical.CreateCounter<long>(
        MetricNames.Technical.AuthLoginAttempts,
        unit: "attempts",
        description: "Number of login attempts");

    private static readonly Counter<long> s_oauthCallbacks = Meters.Technical.CreateCounter<long>(
        MetricNames.Technical.OAuthCallbacks,
        unit: "callbacks",
        description: "Number of OAuth callbacks processed");

    private static readonly Counter<long> s_imageUploads = Meters.Technical.CreateCounter<long>(
        MetricNames.Technical.ImageUploads,
        unit: "uploads",
        description: "Number of image uploads");

    private static readonly Counter<long> s_linkPreviewFetches = Meters.Technical.CreateCounter<long>(
        MetricNames.Technical.LinkPreviewFetches,
        unit: "fetches",
        description: "Number of link preview fetches");

    private static readonly Counter<long> s_novaMessages = Meters.Business.CreateCounter<long>(
        MetricNames.Business.NovaMessages,
        unit: "messages",
        description: "Number of Nova chat messages");

    private static readonly Counter<long> s_novaConversations = Meters.Business.CreateCounter<long>(
        MetricNames.Business.NovaConversations,
        unit: "conversations",
        description: "Number of Nova conversations created");

    private static readonly Counter<long> s_novaTokens = Meters.Business.CreateCounter<long>(
        MetricNames.Business.NovaTokens,
        unit: "tokens",
        description: "Number of tokens used by Nova");

    // Business metric recording methods
    public static void RecordUserRegistered(string? authMethod = null)
    {
        var tags = authMethod is not null
            ? new KeyValuePair<string, object?>(TagNames.Auth.Method, authMethod)
            : default;

        if (authMethod is not null)
            s_usersRegistered.Add(1, tags);
        else
            s_usersRegistered.Add(1);
    }

    public static void RecordUserLoggedIn(string authMethod)
    {
        s_usersLoggedIn.Add(1, new KeyValuePair<string, object?>(TagNames.Auth.Method, authMethod));
    }

    public static void RecordPulseCreated(string pulseType, bool hasImage)
    {
        s_pulsesCreated.Add(1,
            new KeyValuePair<string, object?>(TagNames.Pulse.Type, pulseType),
            new KeyValuePair<string, object?>(TagNames.Pulse.HasImage, hasImage));
    }

    public static void RecordPulseEdited()
    {
        s_pulsesEdited.Add(1);
    }

    public static void RecordPulseDeleted()
    {
        s_pulsesDeleted.Add(1);
    }

    public static void RecordPulseLike()
    {
        s_pulseLikes.Add(1);
    }

    public static void RecordPulseReply()
    {
        s_pulseReplies.Add(1);
    }

    public static void RecordPulseRepulse()
    {
        s_pulseRepulses.Add(1);
    }

    public static void RecordPulseBookmark()
    {
        s_pulseBookmarks.Add(1);
    }

    public static void RecordFollowCreated()
    {
        s_followsCreated.Add(1);
    }

    public static void RecordFollowRemoved()
    {
        s_followsRemoved.Add(1);
    }

    public static void RecordFeedLoad(bool authenticated)
    {
        s_feedLoads.Add(1, new KeyValuePair<string, object?>("authenticated", authenticated));
    }

    public static void RecordProfileView()
    {
        s_profileViews.Add(1);
    }

    public static void RecordProfileSearch()
    {
        s_profileSearches.Add(1);
    }

    public static void RecordNotificationCreated(string notificationType)
    {
        s_notificationsCreated.Add(1, new KeyValuePair<string, object?>("notification_type", notificationType));
    }

    public static void RecordNovaMessage(bool isNewSession, int inputTokens, int outputTokens)
    {
        s_novaMessages.Add(1);

        if (isNewSession)
        {
            s_novaConversations.Add(1);
        }

        s_novaTokens.Add(inputTokens + outputTokens);
    }

    // Technical metric recording methods
    public static void RecordHandlerDuration(string handlerName, double durationMs, bool success)
    {
        s_handlerDuration.Record(durationMs,
            new KeyValuePair<string, object?>(TagNames.Operation.Name, handlerName),
            new KeyValuePair<string, object?>(TagNames.Operation.Success, success));
    }

    public static void RecordValidationDuration(string validatorName, double durationMs, bool isValid)
    {
        s_validationDuration.Record(durationMs,
            new KeyValuePair<string, object?>(TagNames.Validation.ValidatorName, validatorName),
            new KeyValuePair<string, object?>(TagNames.Validation.IsValid, isValid));
    }

    public static void RecordTokenRefresh(bool success)
    {
        s_authTokenRefreshes.Add(1, new KeyValuePair<string, object?>(TagNames.Operation.Success, success));
    }

    public static void RecordLoginAttempt(bool success, string? failureReason = null)
    {
        if (failureReason is not null)
        {
            s_authLoginAttempts.Add(1,
                new KeyValuePair<string, object?>(TagNames.Operation.Success, success),
                new KeyValuePair<string, object?>(TagNames.Auth.FailureReason, failureReason));
        }
        else
        {
            s_authLoginAttempts.Add(1, new KeyValuePair<string, object?>(TagNames.Operation.Success, success));
        }
    }

    public static void RecordOAuthCallback(string provider, bool success)
    {
        s_oauthCallbacks.Add(1,
            new KeyValuePair<string, object?>(TagNames.Auth.Provider, provider),
            new KeyValuePair<string, object?>(TagNames.Operation.Success, success));
    }

    public static void RecordImageUpload(string imageType, bool success)
    {
        s_imageUploads.Add(1,
            new KeyValuePair<string, object?>("image_type", imageType),
            new KeyValuePair<string, object?>(TagNames.Operation.Success, success));
    }

    public static void RecordLinkPreviewFetch(bool success)
    {
        s_linkPreviewFetches.Add(1, new KeyValuePair<string, object?>(TagNames.Operation.Success, success));
    }
}
