namespace Codewrinkles.Application.Nova;

/// <summary>
/// Prompts for summarizing Nova responses for sharing on Pulse.
/// </summary>
public static class SummarizationPrompts
{
    /// <summary>
    /// Maximum length for the summary, leaving room for attribution suffix.
    /// Attribution: "\n\n💡 via #nova" = 15 characters
    /// Pulse limit: 500 characters
    /// Target: 450 characters (buffer for safety, must be complete)
    /// </summary>
    public const int MaxSummaryLength = 450;

    /// <summary>
    /// The attribution suffix added to all shared pulses.
    /// </summary>
    public const string Attribution = "\n\n💡 via #nova";

    /// <summary>
    /// System prompt for summarizing Nova responses for Pulse sharing.
    /// </summary>
    public const string SummarizeForPulseSystem = """
        Distill an AI coach's response into a shareable social media post.

        CRITICAL: Keep it SHORT and COMPLETE.
        - Maximum 400 characters (count carefully!)
        - Must be a COMPLETE thought. Never end mid-sentence or with "..."
        - If you can't fit it, simplify. Less is more.

        RULES:
        1. Write the insight DIRECTLY. No preamble, no formulaic openers
        2. Sound like a real developer sharing a thought on X or BlueSky
        3. ONE takeaway only. Be concise.
        4. Plain text. No markdown, no bullets, no code
        5. 1-2 hashtags at the end. Skip #nova (added automatically)

        NEVER USE:
        - Em dashes (—). Use commas, periods, or parentheses
        - "Just learned...", "Key insight:", "TIL", "Turns out...", "Here's the thing..."
        - Corporate speak: "leverage", "utilize", "best practices"

        GOOD (short, complete):
        - "Dependency injection makes dependencies explicit. It's not just about testing. #dotnet"
        - "async/await is cooperative multitasking. Your code yields control, doesn't spawn threads. #csharp"
        - "High cohesion: related things stay together. Change one thing, don't break unrelated stuff. #softwareengineering"
        """;

    /// <summary>
    /// Builds the user message for summarization with the content to summarize.
    /// </summary>
    /// <param name="content">The Nova response content to summarize.</param>
    /// <returns>The formatted user message.</returns>
    public static string BuildUserMessage(string content)
    {
        return $"""
            Summarize this AI coach response for sharing on social media. Remember: under 485 characters, plain text only, first person voice.

            CONTENT TO SUMMARIZE:
            {content}
            """;
    }
}
