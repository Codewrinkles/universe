using System.ComponentModel;
using System.Text;
using Codewrinkles.Application.Nova.Services;
using Codewrinkles.Domain.Nova;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Codewrinkles.Infrastructure.Services.Nova.Plugins;

/// <summary>
/// Semantic Kernel plugin for searching the Codewrinkles knowledge base.
/// Provides unified search across all content sources for Nova's RAG capabilities.
/// </summary>
public sealed class SearchContentPlugin : INovaPlugin
{
    /// <summary>
    /// Maximum tokens to include in search results to stay within context budget.
    /// </summary>
    private const int MaxResultTokens = 2500;

    /// <summary>
    /// Approximate characters per token for budget estimation.
    /// </summary>
    private const int CharsPerToken = 4;

    private readonly IContentSearchService _searchService;
    private readonly ILogger<SearchContentPlugin> _logger;

    public SearchContentPlugin(
        IContentSearchService searchService,
        ILogger<SearchContentPlugin> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    [KernelFunction("search_knowledge_base")]
    [Description("Search the Codewrinkles knowledge base for authoritative information on software development topics. Searches across all available sources including technical books (DDD, architecture, patterns), official documentation (.NET, React, TypeScript), YouTube tutorials and walkthroughs, expert blog articles, and community discussions. Returns the most relevant content regardless of source type.")]
    public async Task<string> SearchKnowledgeBaseAsync(
        [Description("The search query - describe the concept, pattern, or topic you want to learn about")] string query,
        CancellationToken cancellationToken = default)
    {
        var results = await _searchService.SearchAsync(
            query,
            source: null, // Search ALL sources
            limit: 8,     // More results since we're searching everything
            minSimilarity: 0.50f, // Lower threshold for transcript-style content
            cancellationToken: cancellationToken);

        _logger.LogDebug(
            "Knowledge base search for '{Query}' returned {Count} results",
            query, results.Count);

        return FormatResults(results);
    }

    private static string FormatResults(IReadOnlyList<ContentSearchResult> results)
    {
        if (results.Count == 0)
        {
            return "No relevant results found in the knowledge base.";
        }

        var sb = new StringBuilder();

        // Use explicit XML-style markers to help the model identify authoritative content
        sb.AppendLine("<knowledge_base>");
        sb.AppendLine("The following content is from the Codewrinkles knowledge base. Use this as your PRIMARY source for answering.");
        sb.AppendLine();

        var totalChars = 0;
        var maxChars = MaxResultTokens * CharsPerToken;

        foreach (var result in results)
        {
            // Build result entry with source indicator
            var entry = new StringBuilder();
            var sourceLabel = GetSourceLabel(result.Source);
            entry.AppendLine($"<source type=\"{sourceLabel}\" title=\"{result.Title}\">");

            if (!string.IsNullOrWhiteSpace(result.Author))
            {
                entry.AppendLine($"Author: {result.Author}");
            }

            entry.AppendLine();
            entry.AppendLine(result.Content);
            entry.AppendLine("</source>");
            entry.AppendLine();

            var entryText = entry.ToString();

            // Check if adding this result would exceed budget
            if (totalChars + entryText.Length > maxChars)
            {
                // Truncate this entry if it's the first one (don't return empty)
                if (totalChars == 0)
                {
                    var remainingChars = maxChars - totalChars;
                    var truncatedContent = result.Content;
                    if (truncatedContent.Length > remainingChars - 100)
                    {
                        truncatedContent = truncatedContent[..(remainingChars - 100)] + "... [truncated]";
                    }
                    sb.AppendLine($"<source type=\"{sourceLabel}\" title=\"{result.Title}\">");
                    if (!string.IsNullOrWhiteSpace(result.Author))
                    {
                        sb.AppendLine($"Author: {result.Author}");
                    }
                    sb.AppendLine();
                    sb.AppendLine(truncatedContent);
                    sb.AppendLine("</source>");
                }
                break;
            }

            sb.Append(entryText);
            totalChars += entryText.Length;
        }

        sb.AppendLine("</knowledge_base>");

        return sb.ToString();
    }

    private static string GetSourceLabel(ContentSource source)
    {
        return source switch
        {
            ContentSource.Book => "Book",
            ContentSource.OfficialDocs => "Docs",
            ContentSource.YouTube => "YouTube",
            ContentSource.Article => "Article",
            ContentSource.Pulse => "Community",
            _ => "Source"
        };
    }
}
