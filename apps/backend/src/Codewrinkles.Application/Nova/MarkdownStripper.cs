using System.Text.RegularExpressions;

namespace Codewrinkles.Application.Nova;

/// <summary>
/// Utility to strip markdown syntax from text for LLM pre-processing.
/// Reduces token usage by converting markdown to plain text before summarization.
/// </summary>
public static partial class MarkdownStripper
{
    /// <summary>
    /// Strips markdown syntax from content, converting it to readable plain text.
    /// </summary>
    /// <param name="markdown">The markdown content to strip.</param>
    /// <returns>Plain text without markdown syntax.</returns>
    public static string Strip(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var result = markdown;

        // Remove code blocks (```language ... ```)
        result = CodeBlockRegex().Replace(result, "[code example removed]");

        // Remove inline code (`code`)
        result = InlineCodeRegex().Replace(result, "$1");

        // Remove headers (# Header, ## Header, etc.) - keep the text
        result = HeaderRegex().Replace(result, "$1");

        // Remove bold (**text** or __text__)
        result = BoldRegex().Replace(result, "$1");

        // Remove italic (*text* or _text_) - be careful not to match bullet points
        result = ItalicRegex().Replace(result, "$1");

        // Remove strikethrough (~~text~~)
        result = StrikethroughRegex().Replace(result, "$1");

        // Remove blockquotes (> text) - keep the text
        result = BlockquoteRegex().Replace(result, "$1");

        // Remove horizontal rules (---, ***, ___)
        result = HorizontalRuleRegex().Replace(result, "\n");

        // Convert unordered list items (- item, * item, + item) to sentences
        result = UnorderedListRegex().Replace(result, "$1");

        // Convert ordered list items (1. item, 2. item) to sentences
        result = OrderedListRegex().Replace(result, "$1");

        // Remove link syntax [text](url) - keep text only
        result = LinkRegex().Replace(result, "$1");

        // Remove image syntax ![alt](url)
        result = ImageRegex().Replace(result, "$1");

        // Remove table formatting (| cell | cell |) - simplify to text
        result = TableRowRegex().Replace(result, match =>
        {
            var cells = match.Value
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(c => !TableSeparatorRegex().IsMatch(c));
            return string.Join(", ", cells);
        });

        // Collapse multiple newlines to max 2
        result = MultipleNewlinesRegex().Replace(result, "\n\n");

        // Collapse multiple spaces to single space
        result = MultipleSpacesRegex().Replace(result, " ");

        return result.Trim();
    }

    // Regex patterns using source generators for performance

    [GeneratedRegex(@"```[\s\S]*?```", RegexOptions.Compiled)]
    private static partial Regex CodeBlockRegex();

    [GeneratedRegex(@"`([^`]+)`", RegexOptions.Compiled)]
    private static partial Regex InlineCodeRegex();

    [GeneratedRegex(@"^#{1,6}\s*(.+)$", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"\*\*(.+?)\*\*|__(.+?)__", RegexOptions.Compiled)]
    private static partial Regex BoldRegex();

    [GeneratedRegex(@"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)|(?<!_)_(?!_)(.+?)(?<!_)_(?!_)", RegexOptions.Compiled)]
    private static partial Regex ItalicRegex();

    [GeneratedRegex(@"~~(.+?)~~", RegexOptions.Compiled)]
    private static partial Regex StrikethroughRegex();

    [GeneratedRegex(@"^>\s*(.+)$", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex BlockquoteRegex();

    [GeneratedRegex(@"^[-*_]{3,}\s*$", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex HorizontalRuleRegex();

    [GeneratedRegex(@"^[\s]*[-*+]\s+(.+)$", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex UnorderedListRegex();

    [GeneratedRegex(@"^[\s]*\d+\.\s+(.+)$", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex OrderedListRegex();

    [GeneratedRegex(@"\[([^\]]+)\]\([^\)]+\)", RegexOptions.Compiled)]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"!\[([^\]]*)\]\([^\)]+\)", RegexOptions.Compiled)]
    private static partial Regex ImageRegex();

    [GeneratedRegex(@"^\|.+\|$", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex TableRowRegex();

    [GeneratedRegex(@"^[-:]+$", RegexOptions.Compiled)]
    private static partial Regex TableSeparatorRegex();

    [GeneratedRegex(@"\n{3,}", RegexOptions.Compiled)]
    private static partial Regex MultipleNewlinesRegex();

    [GeneratedRegex(@" {2,}", RegexOptions.Compiled)]
    private static partial Regex MultipleSpacesRegex();
}
