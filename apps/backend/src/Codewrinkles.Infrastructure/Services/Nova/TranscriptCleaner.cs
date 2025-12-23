using System.Text.RegularExpressions;

namespace Codewrinkles.Infrastructure.Services.Nova;

/// <summary>
/// Cleans YouTube transcripts for better RAG performance.
/// Removes timestamps, filler words, section markers, and normalizes text.
/// </summary>
public static partial class TranscriptCleaner
{
    /// <summary>
    /// Filler words and phrases commonly found in spoken transcripts.
    /// These add no semantic value and clutter the content.
    /// </summary>
    private static readonly string[] FillerPhrases =
    [
        // Multi-word fillers (must be checked first, before single words)
        "kind of like",
        "sort of like",
        "you know what I mean",
        "if you know what I mean",
        "you know",
        "I mean",
        "I guess",
        "I think",
        "let's say",
        "so to speak",
        "more or less",
        "in a way",
        "as I said",
        "as we said",
        "like I said",
        "that being said",
        "having said that"
    ];

    /// <summary>
    /// Single filler words that appear frequently in transcripts.
    /// Only removed when they appear as standalone words (not part of larger words).
    /// </summary>
    private static readonly string[] FillerWords =
    [
        "um",
        "uh",
        "ah",
        "eh",
        "er",
        "hmm",
        "basically",
        "actually",
        "literally",
        "obviously",
        "essentially",
        "right",
        "okay",
        "ok",
        "so",
        "well",
        "like",
        "just"
    ];

    /// <summary>
    /// Cleans a YouTube transcript by removing noise and normalizing text.
    /// </summary>
    /// <param name="transcript">The raw transcript text.</param>
    /// <returns>Cleaned transcript optimized for RAG.</returns>
    public static string Clean(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return string.Empty;
        }

        var result = transcript;

        // Step 1: Remove timestamps (e.g., "5:02", "12:34", "1:23:45")
        result = TimestampRegex().Replace(result, " ");

        // Step 2: Remove section markers (e.g., "1. Intro", "2. What are entities?")
        // These are often auto-generated chapter markers from YouTube
        result = SectionMarkerRegex().Replace(result, " ");

        // Step 3: Remove music/applause markers (e.g., "[Music]", "[Applause]", "foreign")
        result = BracketedMarkerRegex().Replace(result, " ");
        result = ForeignMarkerRegex().Replace(result, " ");

        // Step 4: Remove filler phrases (multi-word first)
        foreach (var phrase in FillerPhrases)
        {
            result = Regex.Replace(
                result,
                $@"\b{Regex.Escape(phrase)}\b",
                " ",
                RegexOptions.IgnoreCase);
        }

        // Step 5: Remove standalone filler words
        // Be careful: only remove when they're standalone, not part of other words
        foreach (var word in FillerWords)
        {
            // Match word boundaries, but be more careful with common words
            // that might be meaningful in context
            result = Regex.Replace(
                result,
                $@"(?<=\s|^){Regex.Escape(word)}(?=\s|$|[,.])",
                " ",
                RegexOptions.IgnoreCase);
        }

        // Step 6: Normalize whitespace
        result = MultipleSpacesRegex().Replace(result, " ");

        // Step 7: Fix sentence boundaries
        // Add period after sentences that end abruptly before a capital letter
        result = MissingSentenceEndingRegex().Replace(result, ". $1");

        // Step 8: Remove spaces before punctuation
        result = SpaceBeforePunctuationRegex().Replace(result, "$1");

        // Step 9: Ensure space after punctuation
        result = MissingSpaceAfterPunctuationRegex().Replace(result, "$1 $2");

        // Step 10: Final whitespace cleanup
        result = MultipleSpacesRegex().Replace(result, " ");

        return result.Trim();
    }

    // Matches timestamps like "5:02", "12:34", "1:23:45"
    // Also matches when followed by newline
    [GeneratedRegex(@"\b\d{1,2}:\d{2}(?::\d{2})?\b\s*\n?", RegexOptions.Compiled)]
    private static partial Regex TimestampRegex();

    // Matches section markers like "1. Intro", "2. What are entities?"
    // Common in YouTube auto-generated chapters
    [GeneratedRegex(@"^\s*\d+\.\s+[A-Z][^.!?\n]{0,50}(?:\?|$)", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex SectionMarkerRegex();

    // Matches bracketed markers like "[Music]", "[Applause]", "[Laughter]"
    [GeneratedRegex(@"\[[^\]]*\]", RegexOptions.Compiled)]
    private static partial Regex BracketedMarkerRegex();

    // Matches standalone "foreign" which YouTube uses for non-English speech
    [GeneratedRegex(@"\bforeign\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ForeignMarkerRegex();

    // Matches multiple consecutive spaces
    [GeneratedRegex(@"\s{2,}", RegexOptions.Compiled)]
    private static partial Regex MultipleSpacesRegex();

    // Matches lowercase word followed by space and uppercase (likely missing period)
    [GeneratedRegex(@"([a-z])\s+([A-Z][a-z])", RegexOptions.Compiled)]
    private static partial Regex MissingSentenceEndingRegex();

    // Matches space before punctuation
    [GeneratedRegex(@"\s+([.!?,;:])", RegexOptions.Compiled)]
    private static partial Regex SpaceBeforePunctuationRegex();

    // Matches punctuation not followed by space (except at end)
    [GeneratedRegex(@"([.!?,;:])([A-Za-z])", RegexOptions.Compiled)]
    private static partial Regex MissingSpaceAfterPunctuationRegex();
}
