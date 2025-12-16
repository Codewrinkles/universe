namespace Codewrinkles.Infrastructure.Services.Nova;

/// <summary>
/// Configuration settings for Nova AI service.
/// </summary>
public sealed class NovaSettings
{
    public const string SectionName = "Nova";

    /// <summary>
    /// OpenAI API key. Store in User Secrets, not appsettings.json.
    /// </summary>
    public string OpenAIApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The OpenAI model to use for chat completions.
    /// Default: gpt-4o-mini for cost efficiency.
    /// </summary>
    public string ModelId { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// The OpenAI model to use for text embeddings.
    /// Default: text-embedding-3-small for cost efficiency (1536 dimensions).
    /// </summary>
    public string EmbeddingModelId { get; set; } = "text-embedding-3-small";

    /// <summary>
    /// Maximum tokens in the response.
    /// </summary>
    public int MaxTokens { get; set; } = 2048;

    /// <summary>
    /// Temperature for response randomness (0.0 - 2.0).
    /// Lower = more deterministic, higher = more creative.
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Maximum number of messages to include in conversation context.
    /// Prevents token overflow for long conversations.
    /// </summary>
    public int MaxContextMessages { get; set; } = 20;
}
