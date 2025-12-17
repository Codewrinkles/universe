namespace Codewrinkles.Domain.Nova;

/// <summary>
/// Categories of memories extracted from conversations.
/// Each category has an associated cardinality rule.
/// </summary>
public enum MemoryCategory
{
    /// <summary>
    /// Topics the user asked about. (Multi)
    /// </summary>
    TopicDiscussed = 0,

    /// <summary>
    /// Concepts Nova explained to the user. (Multi)
    /// </summary>
    ConceptExplained = 1,

    /// <summary>
    /// Areas where user showed confusion or difficulty. (Multi)
    /// </summary>
    StruggleIdentified = 2,

    /// <summary>
    /// Areas where user demonstrated competence. (Multi)
    /// </summary>
    StrengthDemonstrated = 3,

    /// <summary>
    /// Specific questions the user asked. (Multi)
    /// </summary>
    QuestionAsked = 4,

    /// <summary>
    /// What user is currently working on or learning. (Single - new supersedes old)
    /// </summary>
    CurrentFocus = 5,

    /// <summary>
    /// What kind of examples resonate with the user. (Single - new supersedes old)
    /// </summary>
    PreferredExamples = 6
}
