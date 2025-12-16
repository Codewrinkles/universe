namespace Codewrinkles.Domain.Nova;

/// <summary>
/// Represents the learner's preferred depth and pace of explanations.
/// </summary>
public enum PreferredPace
{
    /// <summary>
    /// Quick overview - just the essentials, get to the point.
    /// </summary>
    QuickOverview = 0,

    /// <summary>
    /// Balanced - context and examples, moderate depth.
    /// </summary>
    Balanced = 1,

    /// <summary>
    /// Deep dive - thorough explanations, cover edge cases.
    /// </summary>
    DeepDive = 2
}
