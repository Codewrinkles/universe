using Codewrinkles.Domain.Nova;

namespace Codewrinkles.Application.Common.Interfaces;

/// <summary>
/// Abstraction for LLM chat completion services.
/// </summary>
public interface ILlmService
{
    /// <summary>
    /// Generates a chat completion response.
    /// </summary>
    /// <param name="messages">The conversation history.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The assistant's response with metadata.</returns>
    Task<LlmResponse> GetChatCompletionAsync(
        IReadOnlyList<LlmMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a streaming chat completion response.
    /// </summary>
    /// <param name="messages">The conversation history.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of content chunks and final metadata.</returns>
    IAsyncEnumerable<StreamingLlmChunk> GetStreamingChatCompletionAsync(
        IReadOnlyList<LlmMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a chat completion response with tool/function calling support.
    /// The LLM can invoke the provided plugins to retrieve information.
    /// </summary>
    /// <param name="messages">The conversation history.</param>
    /// <param name="plugins">Plugin instances to make available as tools.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The assistant's response with metadata.</returns>
    Task<LlmResponse> GetChatCompletionWithToolsAsync(
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<object> plugins,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a streaming chat completion response with tool/function calling support.
    /// The LLM can invoke the provided plugins to retrieve information.
    /// Note: During tool execution, no chunks are streamed until the tool completes.
    /// </summary>
    /// <param name="messages">The conversation history.</param>
    /// <param name="plugins">Plugin instances to make available as tools.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of content chunks and final metadata.</returns>
    IAsyncEnumerable<StreamingLlmChunk> GetStreamingChatCompletionWithToolsAsync(
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<object> plugins,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a message for the LLM.
/// </summary>
public sealed record LlmMessage(MessageRole Role, string Content);

/// <summary>
/// Represents a complete LLM response.
/// </summary>
public sealed record LlmResponse(
    string Content,
    int InputTokens,
    int OutputTokens,
    string ModelUsed);

/// <summary>
/// Represents a chunk in a streaming LLM response.
/// </summary>
public sealed record StreamingLlmChunk(
    string Content,
    bool IsComplete,
    int? InputTokens = null,
    int? OutputTokens = null,
    string? ModelUsed = null);
