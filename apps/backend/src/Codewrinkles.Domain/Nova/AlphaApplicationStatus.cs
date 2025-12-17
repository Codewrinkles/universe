namespace Codewrinkles.Domain.Nova;

/// <summary>
/// Status of an Alpha application.
/// </summary>
public enum AlphaApplicationStatus
{
    /// <summary>
    /// Application is pending review.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Application was accepted, invite code generated.
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// Application was waitlisted (not rejected, just deferred).
    /// </summary>
    Waitlisted = 2
}
