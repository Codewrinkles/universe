using System.Runtime.CompilerServices;
using System.Text;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Codewrinkles.Infrastructure.Services.Nova;

/// <summary>
/// LLM service implementation using Microsoft Semantic Kernel with OpenAI.
/// </summary>
public sealed class SemanticKernelLlmService : ILlmService
{
    private readonly Kernel _baseKernel;
    private readonly IChatCompletionService _chatService;
    private readonly NovaSettings _settings;

    public SemanticKernelLlmService(
        Kernel kernel,
        IOptions<NovaSettings> settings)
    {
        _baseKernel = kernel;
        _chatService = kernel.GetRequiredService<IChatCompletionService>();
        _settings = settings.Value;
    }

    public async Task<LlmResponse> GetChatCompletionAsync(
        IReadOnlyList<LlmMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var chatHistory = BuildChatHistory(messages);
        var executionSettings = CreateExecutionSettings();

        var response = await _chatService.GetChatMessageContentAsync(
            chatHistory,
            executionSettings,
            cancellationToken: cancellationToken);

        // Extract token usage from metadata
        var inputTokens = 0;
        var outputTokens = 0;

        if (response.Metadata?.TryGetValue("Usage", out var usageObj) == true &&
            usageObj is OpenAI.Chat.ChatTokenUsage usage)
        {
            inputTokens = usage.InputTokenCount;
            outputTokens = usage.OutputTokenCount;
        }

        return new LlmResponse(
            Content: response.Content ?? string.Empty,
            InputTokens: inputTokens,
            OutputTokens: outputTokens,
            ModelUsed: _settings.ModelId);
    }

    public async IAsyncEnumerable<StreamingLlmChunk> GetStreamingChatCompletionAsync(
        IReadOnlyList<LlmMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatHistory = BuildChatHistory(messages);
        var executionSettings = CreateExecutionSettings();

        var fullContent = new StringBuilder();
        var inputTokens = 0;
        var outputTokens = 0;

        await foreach (var chunk in _chatService.GetStreamingChatMessageContentsAsync(
            chatHistory,
            executionSettings,
            cancellationToken: cancellationToken))
        {
            var content = chunk.Content ?? string.Empty;
            fullContent.Append(content);

            // Try to get token usage from final chunk
            if (chunk.Metadata?.TryGetValue("Usage", out var usageObj) == true &&
                usageObj is OpenAI.Chat.ChatTokenUsage usage)
            {
                inputTokens = usage.InputTokenCount;
                outputTokens = usage.OutputTokenCount;
            }

            // Yield content chunks
            if (!string.IsNullOrEmpty(content))
            {
                yield return new StreamingLlmChunk(content, IsComplete: false);
            }
        }

        // Yield final chunk with metadata
        yield return new StreamingLlmChunk(
            Content: string.Empty,
            IsComplete: true,
            InputTokens: inputTokens,
            OutputTokens: outputTokens,
            ModelUsed: _settings.ModelId);
    }

    private ChatHistory BuildChatHistory(IReadOnlyList<LlmMessage> messages)
    {
        var chatHistory = new ChatHistory();

        foreach (var message in messages)
        {
            var role = message.Role switch
            {
                MessageRole.User => AuthorRole.User,
                MessageRole.Assistant => AuthorRole.Assistant,
                MessageRole.System => AuthorRole.System,
                _ => throw new ArgumentOutOfRangeException(nameof(message.Role))
            };

            chatHistory.Add(new ChatMessageContent(role, message.Content));
        }

        return chatHistory;
    }

    private OpenAIPromptExecutionSettings CreateExecutionSettings()
    {
        return new OpenAIPromptExecutionSettings
        {
            MaxTokens = _settings.MaxTokens,
            Temperature = _settings.Temperature
        };
    }

    public async Task<LlmResponse> GetChatCompletionWithToolsAsync(
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<object> plugins,
        CancellationToken cancellationToken = default)
    {
        var chatHistory = BuildChatHistory(messages);
        var kernelWithPlugins = CreateKernelWithPlugins(plugins);
        var executionSettings = CreateExecutionSettingsWithTools();

        var response = await _chatService.GetChatMessageContentAsync(
            chatHistory,
            executionSettings,
            kernel: kernelWithPlugins,
            cancellationToken: cancellationToken);

        // Extract token usage from metadata
        var inputTokens = 0;
        var outputTokens = 0;

        if (response.Metadata?.TryGetValue("Usage", out var usageObj) == true &&
            usageObj is OpenAI.Chat.ChatTokenUsage usage)
        {
            inputTokens = usage.InputTokenCount;
            outputTokens = usage.OutputTokenCount;
        }

        return new LlmResponse(
            Content: response.Content ?? string.Empty,
            InputTokens: inputTokens,
            OutputTokens: outputTokens,
            ModelUsed: _settings.ModelId);
    }

    public async IAsyncEnumerable<StreamingLlmChunk> GetStreamingChatCompletionWithToolsAsync(
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<object> plugins,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatHistory = BuildChatHistory(messages);
        var kernelWithPlugins = CreateKernelWithPlugins(plugins);
        var executionSettings = CreateExecutionSettingsWithTools();

        var fullContent = new StringBuilder();
        var inputTokens = 0;
        var outputTokens = 0;

        await foreach (var chunk in _chatService.GetStreamingChatMessageContentsAsync(
            chatHistory,
            executionSettings,
            kernel: kernelWithPlugins,
            cancellationToken: cancellationToken))
        {
            var content = chunk.Content ?? string.Empty;
            fullContent.Append(content);

            // Try to get token usage from final chunk
            if (chunk.Metadata?.TryGetValue("Usage", out var usageObj) == true &&
                usageObj is OpenAI.Chat.ChatTokenUsage usage)
            {
                inputTokens = usage.InputTokenCount;
                outputTokens = usage.OutputTokenCount;
            }

            // Yield content chunks
            if (!string.IsNullOrEmpty(content))
            {
                yield return new StreamingLlmChunk(content, IsComplete: false);
            }
        }

        // Yield final chunk with metadata
        yield return new StreamingLlmChunk(
            Content: string.Empty,
            IsComplete: true,
            InputTokens: inputTokens,
            OutputTokens: outputTokens,
            ModelUsed: _settings.ModelId);
    }

    private Kernel CreateKernelWithPlugins(IReadOnlyList<object> plugins)
    {
        // Create a new kernel builder and add the chat completion service
        var builder = Kernel.CreateBuilder();

        // Add the OpenAI chat completion service using the same settings
        builder.AddOpenAIChatCompletion(
            modelId: _settings.ModelId,
            apiKey: _settings.OpenAIApiKey);

        var kernel = builder.Build();

        // Import each plugin
        foreach (var plugin in plugins)
        {
            var pluginName = plugin.GetType().Name;
            kernel.ImportPluginFromObject(plugin, pluginName);
        }

        return kernel;
    }

    private OpenAIPromptExecutionSettings CreateExecutionSettingsWithTools()
    {
        return new OpenAIPromptExecutionSettings
        {
            MaxTokens = _settings.MaxTokens,
            Temperature = _settings.Temperature,
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };
    }
}
