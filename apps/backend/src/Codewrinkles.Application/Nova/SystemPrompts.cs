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

        ## Your Expertise
        Software architecture, .NET/C#, React/TypeScript, databases, system design, and helping developers level up their careers. You've seen patterns succeed and fail in real codebases.

        ## How to Help
        - Give your actual take first, then explain the reasoning
        - Use concrete examples from real development scenarios
        - When there are multiple valid approaches, explain when you'd pick each one
        - Ask follow-up questions when the context would change your answer
        - If you don't know, say so
        """;

    /// <summary>
    /// Builds a personalized system prompt by injecting learner profile data.
    /// </summary>
    /// <param name="profile">The learner's profile, or null if no profile exists.</param>
    /// <returns>The complete system prompt with personalization.</returns>
    public static string BuildPersonalizedPrompt(LearnerProfile? profile)
    {
        if (profile is null || !profile.HasUserData())
        {
            return CodyCoachBase;
        }

        var personalization = new StringBuilder();
        personalization.AppendLine();
        personalization.AppendLine("## About This Learner");

        // Professional background
        if (!string.IsNullOrWhiteSpace(profile.CurrentRole))
        {
            personalization.Append($"- Role: {profile.CurrentRole}");
            if (profile.ExperienceYears.HasValue)
            {
                personalization.Append($" ({profile.ExperienceYears} years experience)");
            }
            personalization.AppendLine();
        }
        else if (profile.ExperienceYears.HasValue)
        {
            personalization.AppendLine($"- Experience: {profile.ExperienceYears} years");
        }

        if (!string.IsNullOrWhiteSpace(profile.PrimaryTechStack))
        {
            personalization.AppendLine($"- Tech stack: {profile.PrimaryTechStack}");
        }

        if (!string.IsNullOrWhiteSpace(profile.CurrentProject))
        {
            personalization.AppendLine($"- Working on: {profile.CurrentProject}");
        }

        // Learning context
        if (!string.IsNullOrWhiteSpace(profile.LearningGoals))
        {
            personalization.AppendLine($"- Learning goals: {profile.LearningGoals}");
        }

        // Learning preferences
        if (profile.LearningStyle.HasValue || profile.PreferredPace.HasValue)
        {
            personalization.AppendLine();
            personalization.AppendLine("## Teaching Style Preferences");

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
                    personalization.AppendLine($"- {styleDescription}");
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
                    personalization.AppendLine($"- {paceDescription}");
                }
            }
        }

        // AI-identified insights (if available)
        if (!string.IsNullOrWhiteSpace(profile.IdentifiedStrengths) ||
            !string.IsNullOrWhiteSpace(profile.IdentifiedStruggles))
        {
            personalization.AppendLine();
            personalization.AppendLine("## Observed Patterns (from past conversations)");

            if (!string.IsNullOrWhiteSpace(profile.IdentifiedStrengths))
            {
                personalization.AppendLine($"- Strengths: {profile.IdentifiedStrengths}");
            }

            if (!string.IsNullOrWhiteSpace(profile.IdentifiedStruggles))
            {
                personalization.AppendLine($"- Areas to focus on: {profile.IdentifiedStruggles}");
            }
        }

        return CodyCoachBase + personalization;
    }
}
