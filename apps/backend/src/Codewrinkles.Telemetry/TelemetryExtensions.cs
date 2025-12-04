using System.Diagnostics;
using OpenTelemetry.Trace;

namespace Codewrinkles.Telemetry;

/// <summary>
/// Extension methods for telemetry operations.
/// </summary>
public static class TelemetryExtensions
{
    /// <summary>
    /// Sets both IdentityId and ProfileId tags on an activity.
    /// </summary>
    public static Activity? SetUserContext(this Activity? activity, Guid? identityId, Guid? profileId)
    {
        if (activity is null) return null;

        if (identityId.HasValue)
        {
            activity.SetTag(TagNames.User.IdentityId, identityId.Value.ToString());
        }

        if (profileId.HasValue)
        {
            activity.SetTag(TagNames.User.ProfileId, profileId.Value.ToString());
        }

        return activity;
    }

    /// <summary>
    /// Sets the ProfileId tag on an activity.
    /// </summary>
    public static Activity? SetProfileId(this Activity? activity, Guid profileId)
    {
        return activity?.SetTag(TagNames.User.ProfileId, profileId.ToString());
    }

    /// <summary>
    /// Sets the IdentityId tag on an activity.
    /// </summary>
    public static Activity? SetIdentityId(this Activity? activity, Guid identityId)
    {
        return activity?.SetTag(TagNames.User.IdentityId, identityId.ToString());
    }

    /// <summary>
    /// Sets operation success tag on an activity.
    /// </summary>
    public static Activity? SetSuccess(this Activity? activity, bool success)
    {
        return activity?.SetTag(TagNames.Operation.Success, success);
    }

    /// <summary>
    /// Sets entity type and ID tags on an activity.
    /// </summary>
    public static Activity? SetEntity(this Activity? activity, string entityType, Guid entityId)
    {
        activity?.SetTag(TagNames.Entity.Type, entityType);
        activity?.SetTag(TagNames.Entity.Id, entityId.ToString());
        return activity;
    }

    /// <summary>
    /// Sets database record count tag on an activity.
    /// </summary>
    public static Activity? SetRecordCount(this Activity? activity, int count)
    {
        return activity?.SetTag(TagNames.Database.RecordCount, count);
    }

    /// <summary>
    /// Sets feed result metadata tags on an activity.
    /// </summary>
    public static Activity? SetFeedMetadata(this Activity? activity, int resultCount, bool hasMore, string? cursorType = null)
    {
        activity?.SetTag(TagNames.Feed.ResultCount, resultCount);
        activity?.SetTag(TagNames.Feed.HasMoreResults, hasMore);
        if (cursorType is not null)
        {
            activity?.SetTag(TagNames.Feed.CursorType, cursorType);
        }
        return activity;
    }

    /// <summary>
    /// Sets pulse metadata tags on an activity.
    /// </summary>
    public static Activity? SetPulseMetadata(this Activity? activity, string pulseType, bool hasImage, bool hasLink, int mentionCount = 0, int hashtagCount = 0)
    {
        activity?.SetTag(TagNames.Pulse.Type, pulseType);
        activity?.SetTag(TagNames.Pulse.HasImage, hasImage);
        activity?.SetTag(TagNames.Pulse.HasLink, hasLink);
        activity?.SetTag(TagNames.Pulse.MentionCount, mentionCount);
        activity?.SetTag(TagNames.Pulse.HashtagCount, hashtagCount);
        return activity;
    }

    /// <summary>
    /// Sets validation result tags on an activity.
    /// </summary>
    public static Activity? SetValidationResult(this Activity? activity, string validatorName, bool isValid, int errorCount = 0)
    {
        activity?.SetTag(TagNames.Validation.ValidatorName, validatorName);
        activity?.SetTag(TagNames.Validation.IsValid, isValid);
        activity?.SetTag(TagNames.Validation.ErrorCount, errorCount);
        return activity;
    }

    /// <summary>
    /// Records an exception on the activity.
    /// </summary>
    public static Activity? RecordError(this Activity? activity, Exception exception)
    {
        if (activity is null) return null;

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.SetTag(TagNames.Error.Type, exception.GetType().Name);
        activity.SetTag(TagNames.Error.Message, exception.Message);
        activity.AddException(exception);

        return activity;
    }

    /// <summary>
    /// Extracts email domain from an email address for logging (privacy-safe).
    /// </summary>
    public static string GetEmailDomain(string email)
    {
        var atIndex = email.IndexOf('@');
        return atIndex >= 0 ? email[atIndex..] : "unknown";
    }

    /// <summary>
    /// Sets email domain tag (privacy-safe - only logs domain, not full email).
    /// </summary>
    public static Activity? SetEmailDomain(this Activity? activity, string email)
    {
        return activity?.SetTag(TagNames.User.EmailDomain, GetEmailDomain(email));
    }

    /// <summary>
    /// Sets OAuth provider tag.
    /// </summary>
    public static Activity? SetOAuthProvider(this Activity? activity, string provider)
    {
        return activity?.SetTag(TagNames.Auth.Provider, provider);
    }

    /// <summary>
    /// Starts an activity for Application layer operations.
    /// Returns null if no listener is active (sampling).
    /// </summary>
    public static Activity? StartApplicationActivity(string spanName, ActivityKind kind = ActivityKind.Internal)
    {
        return ActivitySources.Application.StartActivity(spanName, kind);
    }

    /// <summary>
    /// Starts an activity for Infrastructure layer operations.
    /// Returns null if no listener is active (sampling).
    /// </summary>
    public static Activity? StartInfrastructureActivity(string spanName, ActivityKind kind = ActivityKind.Internal)
    {
        return ActivitySources.Infrastructure.StartActivity(spanName, kind);
    }
}
