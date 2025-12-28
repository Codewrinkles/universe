using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Kommand;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

/// <summary>
/// Command to summarize a Nova response for sharing on Pulse.
/// </summary>
public sealed record SummarizeForPulseCommand(
    Guid ProfileId,
    string Content
) : ICommand<SummarizeForPulseResult>;

/// <summary>
/// Result containing the summarized content ready for Pulse.
/// </summary>
public sealed record SummarizeForPulseResult(string Summary);

/// <summary>
/// Validates the summarization request.
/// </summary>
public sealed class SummarizeForPulseValidator : IValidator<SummarizeForPulseCommand>
{
    private List<ValidationError> _errors = null!;

    public Task<ValidationResult> ValidateAsync(
        SummarizeForPulseCommand command,
        CancellationToken cancellationToken)
    {
        _errors = [];

        ValidateContent(command.Content);

        return Task.FromResult(_errors.Count > 0
            ? ValidationResult.Failure(_errors)
            : ValidationResult.Success());
    }

    private void ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            _errors.Add(new ValidationError(
                nameof(SummarizeForPulseCommand.Content),
                "Content to summarize is required"));
        }
    }
}

/// <summary>
/// Handles summarizing Nova responses for Pulse sharing.
/// </summary>
public sealed class SummarizeForPulseCommandHandler
    : ICommandHandler<SummarizeForPulseCommand, SummarizeForPulseResult>
{
    private readonly ILlmService _llmService;

    public SummarizeForPulseCommandHandler(ILlmService llmService)
    {
        _llmService = llmService;
    }

    public async Task<SummarizeForPulseResult> HandleAsync(
        SummarizeForPulseCommand command,
        CancellationToken cancellationToken)
    {
        // Pre-process: strip markdown to reduce tokens and help LLM focus on content
        var strippedContent = MarkdownStripper.Strip(command.Content);

        // Build LLM messages for summarization
        var messages = new List<LlmMessage>
        {
            new(MessageRole.System, SummarizationPrompts.SummarizeForPulseSystem),
            new(MessageRole.User, SummarizationPrompts.BuildUserMessage(strippedContent))
        };

        // Get summarization from LLM (using smaller model for cost efficiency)
        var response = await _llmService.GetChatCompletionAsync(messages, cancellationToken);

        // Clean and validate the summary
        var summary = response.Content.Trim();

        // Remove any trailing "..." the LLM might have added (sign of incomplete thought)
        summary = summary.TrimEnd('.').TrimEnd() + (summary.EndsWith("...") ? "" : "");
        if (summary.EndsWith(".."))
        {
            summary = summary[..^2].TrimEnd();
        }

        // Ensure the summary ends with a complete thought
        summary = EnsureCompleteSentence(summary, SummarizationPrompts.MaxSummaryLength);

        return new SummarizeForPulseResult(summary);
    }

    /// <summary>
    /// Ensures the text ends with a complete sentence within the character limit.
    /// Finds the last sentence-ending punctuation or hashtag and cuts there.
    /// </summary>
    private static string EnsureCompleteSentence(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        // If already within limit and ends properly, return as-is
        if (text.Length <= maxLength && EndsWithCompleteSentence(text))
        {
            return text;
        }

        // If over limit, we need to truncate to the last complete sentence
        var workingText = text.Length > maxLength ? text[..maxLength] : text;

        // Find the last position that ends a complete thought
        // Priority: hashtag at end, then sentence-ending punctuation
        var lastHashtag = FindLastHashtagEnd(workingText);
        var lastSentenceEnd = FindLastSentenceEnd(workingText);

        // Use whichever is later (closer to the end)
        var cutPoint = Math.Max(lastHashtag, lastSentenceEnd);

        if (cutPoint > 0)
        {
            return workingText[..cutPoint].TrimEnd();
        }

        // Fallback: if no good cut point found, take the whole thing up to limit
        // and try to end at a word boundary
        if (workingText.Length > maxLength)
        {
            var lastSpace = workingText.LastIndexOf(' ', maxLength - 1);
            if (lastSpace > maxLength / 2)
            {
                return workingText[..lastSpace].TrimEnd();
            }
        }

        return workingText.TrimEnd();
    }

    /// <summary>
    /// Checks if text ends with proper sentence-ending punctuation or a hashtag.
    /// </summary>
    private static bool EndsWithCompleteSentence(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.TrimEnd();
        if (trimmed.Length == 0)
        {
            return false;
        }

        var lastChar = trimmed[^1];

        // Ends with punctuation
        if (lastChar is '.' or '!' or '?')
        {
            return true;
        }

        // Ends with a hashtag (e.g., "#dotnet")
        // Check if last word starts with #
        var lastSpace = trimmed.LastIndexOf(' ');
        if (lastSpace >= 0)
        {
            var lastWord = trimmed[(lastSpace + 1)..];
            if (lastWord.StartsWith('#') && lastWord.Length > 1)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Finds the position after the last complete hashtag in the text.
    /// </summary>
    private static int FindLastHashtagEnd(string text)
    {
        // Look for pattern: space + # + word characters + (end or space)
        var lastHashStart = text.LastIndexOf(" #", StringComparison.Ordinal);
        if (lastHashStart < 0)
        {
            // Check if text starts with hashtag
            if (text.StartsWith('#'))
            {
                lastHashStart = -1; // Will become 0 after +1
            }
            else
            {
                return -1;
            }
        }

        var hashStart = lastHashStart + 1;
        var hashEnd = hashStart + 1;

        // Find end of hashtag (next space or end of string)
        while (hashEnd < text.Length && !char.IsWhiteSpace(text[hashEnd]))
        {
            hashEnd++;
        }

        // Validate it's a real hashtag (has at least one char after #)
        if (hashEnd - hashStart > 1)
        {
            return hashEnd;
        }

        return -1;
    }

    /// <summary>
    /// Finds the position after the last sentence-ending punctuation.
    /// </summary>
    private static int FindLastSentenceEnd(string text)
    {
        var lastEnd = -1;

        for (var i = text.Length - 1; i >= 0; i--)
        {
            if (text[i] is '.' or '!' or '?')
            {
                // Make sure it's not "..." (ellipsis)
                if (text[i] == '.' && i >= 2 && text[i - 1] == '.' && text[i - 2] == '.')
                {
                    continue;
                }

                lastEnd = i + 1;
                break;
            }
        }

        return lastEnd;
    }
}
