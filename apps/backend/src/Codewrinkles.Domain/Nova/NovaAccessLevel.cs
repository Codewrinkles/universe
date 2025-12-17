namespace Codewrinkles.Domain.Nova;

/// <summary>
/// Defines the access level a user has for Nova.
/// </summary>
public enum NovaAccessLevel
{
    /// <summary>
    /// No access to Nova.
    /// </summary>
    None = 0,

    /// <summary>
    /// Alpha tester - free unlimited access during Alpha period.
    /// </summary>
    Alpha = 1,

    /// <summary>
    /// Free tier - limited features, post-Alpha.
    /// </summary>
    Free = 2,

    /// <summary>
    /// Pro tier - full features, paid subscription.
    /// </summary>
    Pro = 3,

    /// <summary>
    /// Lifetime access - one-time purchase, full features forever.
    /// </summary>
    Lifetime = 4
}
