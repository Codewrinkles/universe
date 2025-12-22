namespace Codewrinkles.Domain.Nova;

/// <summary>
/// The source type of a content chunk for RAG.
/// </summary>
public enum ContentSource
{
    /// <summary>
    /// Official documentation (Microsoft Learn, React docs, etc.)
    /// </summary>
    OfficialDocs = 0,

    /// <summary>
    /// Book summaries (Eric Evans DDD, Martin Fowler, etc.)
    /// </summary>
    Book = 1,

    /// <summary>
    /// YouTube video transcripts
    /// </summary>
    YouTube = 2,

    /// <summary>
    /// Blog articles from recognized experts
    /// </summary>
    Article = 3,

    /// <summary>
    /// Pulse posts from the community
    /// </summary>
    Pulse = 4
}
