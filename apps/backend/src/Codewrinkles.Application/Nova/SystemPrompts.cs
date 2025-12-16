using System.Text;
using Codewrinkles.Domain.Nova;

namespace Codewrinkles.Application.Nova;

/// <summary>
/// System prompts for Nova's AI coaching personality.
/// </summary>
public static class SystemPrompts
{
    /// <summary>
    /// The base system prompt that defines Cody's personality and behavior.
    /// </summary>
    private const string CodyCoachBase = """
        You are Cody, an AI learning coach created by Dan from Codewrinkles. You help developers grow their technical skills through conversation.

        ## Your Voice
        You sound like a senior developer friend who's been through the trenches. Conversational, not formal. You get straight to the point without being curt. You're genuinely interested in helping people understand things deeply, not just giving them answers to copy-paste.

        You have opinions and you share them. When something is a bad idea, you say so. When there's nuance, you explain the trade-offs honestly. You don't hedge everything with "it depends" - you say what it actually depends on.

        Vary how you start responses naturally - sometimes dive straight into the answer, sometimes set up context first, sometimes ask a clarifying question. Never start responses the same way twice in a row.

        ## What NOT to do
        - No "Great question!" or "I'd be happy to help!" - just help
        - No "It's important to note that..." - just say it
        - No corporate speak - no "leverage", "synergy", "best practices", "utilize"
        - No bullet point soup when a few sentences would be clearer
        - No forced summaries or "Let me know if you have questions!"
        - Don't repeat the same opening phrases
        - Never use em dashes (—) - use commas, parentheses, or separate sentences instead

        ## Your Expertise
        Software architecture, .NET/C#, React/TypeScript, databases, system design, and helping developers level up their careers. You've seen patterns succeed and fail in real codebases.

        ## How to Help
        - Give your actual take first, then explain the reasoning
        - Use concrete examples from real development scenarios
        - When there are multiple valid approaches, explain when you'd pick each one
        - Ask follow-up questions when the context would change your answer
        - If you don't know, say so

        ## Technical Precision
        - Distinguish related but different concepts explicitly (e.g., Event Sourcing vs Event Driven Architecture, Authentication vs Authorization, Concurrency vs Parallelism)
        - Don't assume the "conference talk version" is correct - many tutorials conflate concepts for simplicity
        - If a topic is often confused with another, proactively clarify the distinction
        - When a user conflates concepts, gently correct them
        - When YOU get something wrong and get called out, own it directly without being patronizing - no "great point!" or "you're thinking critically!" - just acknowledge the mistake and correct it
        """;

    /// <summary>
    /// Builds a personalized system prompt by injecting learner profile data and memories.
    /// </summary>
    /// <param name="profile">The learner's profile, or null if no profile exists.</param>
    /// <param name="memories">Optional memories from past conversations.</param>
    /// <returns>The complete system prompt with personalization.</returns>
    public static string BuildPersonalizedPrompt(
        LearnerProfile? profile,
        IReadOnlyList<MemoryDto>? memories = null)
    {
        var hasProfile = profile is not null && profile.HasUserData();
        var hasMemories = memories is not null && memories.Count > 0;

        if (!hasProfile && !hasMemories)
        {
            return CodyCoachBase;
        }

        var prompt = new StringBuilder(CodyCoachBase);

        // Add profile personalization
        if (hasProfile)
        {
            AppendProfilePersonalization(prompt, profile!);
        }

        // Add memory context
        if (hasMemories)
        {
            prompt.Append(BuildMemoryContext(memories!));
        }

        return prompt.ToString();
    }

    /// <summary>
    /// Builds the memory context section for the system prompt.
    /// </summary>
    public static string BuildMemoryContext(IReadOnlyList<MemoryDto> memories)
    {
        if (memories.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("## What I Remember From Our Past Conversations");

        // Group memories by category
        var topics = memories.Where(m => m.Category == nameof(MemoryCategory.TopicDiscussed)).ToList();
        var concepts = memories.Where(m => m.Category == nameof(MemoryCategory.ConceptExplained)).ToList();
        var struggles = memories.Where(m => m.Category == nameof(MemoryCategory.StruggleIdentified)).ToList();
        var strengths = memories.Where(m => m.Category == nameof(MemoryCategory.StrengthDemonstrated)).ToList();
        var focus = memories.FirstOrDefault(m => m.Category == nameof(MemoryCategory.CurrentFocus));

        if (focus is not null)
        {
            sb.AppendLine($"- Current focus: {focus.Content}");
        }

        if (topics.Count > 0)
        {
            sb.AppendLine($"- Topics we've discussed: {string.Join(", ", topics.Select(t => t.Content))}");
        }

        if (concepts.Count > 0)
        {
            sb.AppendLine($"- Concepts I've explained: {string.Join(", ", concepts.Select(c => c.Content))}");
        }

        if (strengths.Count > 0)
        {
            sb.AppendLine($"- Your strengths I've observed: {string.Join(", ", strengths.Select(s => s.Content))}");
        }

        if (struggles.Count > 0)
        {
            sb.AppendLine($"- Areas to reinforce: {string.Join(", ", struggles.Select(s => s.Content))}");
        }

        return sb.ToString();
    }

    private static void AppendProfilePersonalization(StringBuilder prompt, LearnerProfile profile)
    {
        prompt.AppendLine();
        prompt.AppendLine("## About This Learner");

        // Professional background
        if (!string.IsNullOrWhiteSpace(profile.CurrentRole))
        {
            prompt.Append($"- Role: {profile.CurrentRole}");
            if (profile.ExperienceYears.HasValue)
            {
                prompt.Append($" ({profile.ExperienceYears} years experience)");
            }
            prompt.AppendLine();
        }
        else if (profile.ExperienceYears.HasValue)
        {
            prompt.AppendLine($"- Experience: {profile.ExperienceYears} years");
        }

        if (!string.IsNullOrWhiteSpace(profile.PrimaryTechStack))
        {
            prompt.AppendLine($"- Tech stack: {profile.PrimaryTechStack}");
        }

        if (!string.IsNullOrWhiteSpace(profile.CurrentProject))
        {
            prompt.AppendLine($"- Working on: {profile.CurrentProject}");
        }

        // Learning context
        if (!string.IsNullOrWhiteSpace(profile.LearningGoals))
        {
            prompt.AppendLine($"- Learning goals: {profile.LearningGoals}");
        }

        // Learning preferences
        if (profile.LearningStyle.HasValue || profile.PreferredPace.HasValue)
        {
            prompt.AppendLine();
            prompt.AppendLine("## Teaching Style Preferences");

            if (profile.LearningStyle.HasValue)
            {
                var styleDescription = profile.LearningStyle.Value switch
                {
                    LearningStyle.ExamplesFirst => "Show code examples first, then explain the theory behind them",
                    LearningStyle.TheoryFirst => "Explain concepts and theory first, then show examples",
                    LearningStyle.HandsOn => "Let them try things and fail, then explain what went wrong",
                    _ => null
                };

                if (styleDescription is not null)
                {
                    prompt.AppendLine($"- {styleDescription}");
                }
            }

            if (profile.PreferredPace.HasValue)
            {
                var paceDescription = profile.PreferredPace.Value switch
                {
                    PreferredPace.QuickOverview => "Keep explanations concise - just the essentials, get to the point",
                    PreferredPace.Balanced => "Provide moderate depth with context and examples",
                    PreferredPace.DeepDive => "Give thorough explanations, cover edge cases and nuances",
                    _ => null
                };

                if (paceDescription is not null)
                {
                    prompt.AppendLine($"- {paceDescription}");
                }
            }
        }

        // AI-identified insights (if available)
        if (!string.IsNullOrWhiteSpace(profile.IdentifiedStrengths) ||
            !string.IsNullOrWhiteSpace(profile.IdentifiedStruggles))
        {
            prompt.AppendLine();
            prompt.AppendLine("## Observed Patterns (from past conversations)");

            if (!string.IsNullOrWhiteSpace(profile.IdentifiedStrengths))
            {
                prompt.AppendLine($"- Strengths: {profile.IdentifiedStrengths}");
            }

            if (!string.IsNullOrWhiteSpace(profile.IdentifiedStruggles))
            {
                prompt.AppendLine($"- Areas to focus on: {profile.IdentifiedStruggles}");
            }
        }
    }
}
