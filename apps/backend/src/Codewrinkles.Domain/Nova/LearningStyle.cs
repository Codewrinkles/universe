namespace Codewrinkles.Domain.Nova;

/// <summary>
/// Represents how a learner prefers to learn new concepts.
/// </summary>
public enum LearningStyle
{
    /// <summary>
    /// Show code examples first, then explain the theory.
    /// </summary>
    ExamplesFirst = 0,

    /// <summary>
    /// Explain the theory and concepts, then show examples.
    /// </summary>
    TheoryFirst = 1,

    /// <summary>
    /// Let the learner try and fail, then explain what went wrong.
    /// </summary>
    HandsOn = 2
}
