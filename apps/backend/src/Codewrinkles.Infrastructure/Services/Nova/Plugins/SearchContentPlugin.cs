using System.ComponentModel;
using System.Text;
using Codewrinkles.Application.Nova.Services;
using Codewrinkles.Domain.Nova;
using Microsoft.SemanticKernel;

namespace Codewrinkles.Infrastructure.Services.Nova.Plugins;

/// <summary>
/// Semantic Kernel plugin for searching content from various sources.
/// Used by Nova to retrieve authoritative information when answering questions.
/// </summary>
public sealed class SearchContentPlugin : INovaPlugin
{
    /// <summary>
    /// Maximum tokens to include in search results to stay within context budget.
    /// </summary>
    private const int MaxResultTokens = 2000;

    /// <summary>
    /// Approximate characters per token for budget estimation.
    /// </summary>
    private const int CharsPerToken = 4;

    private readonly IContentSearchService _searchService;

    public SearchContentPlugin(IContentSearchService searchService)
    {
        _searchService = searchService;
    }

    [KernelFunction("search_books")]
    [Description("Search book content for architecture patterns, design principles, DDD, SOLID, and software methodologies. Use for conceptual and architectural questions. Sources include Eric Evans' DDD, Martin Fowler's patterns, and similar authoritative texts.")]
    public async Task<string> SearchBooksAsync(
        [Description("The search query - be specific about the concept or pattern you're looking for")] string query,
        [Description("Optional: filter by author name (e.g., 'Eric Evans', 'Martin Fowler')")] string? author = null,
        CancellationToken cancellationToken = default)
    {
        var results = await _searchService.SearchAsync(
            query,
            source: ContentSource.Book,
            author: author,
            limit: 5,
            minSimilarity: 0.65f,
            cancellationToken: cancellationToken);

        return FormatResults(results, "books");
    }

    [KernelFunction("search_official_docs")]
    [Description("Search official documentation for API references, syntax, and framework-specific guidance. Use for technical implementation questions. Supports .NET, C#, ASP.NET Core, EF Core, React, and TypeScript documentation.")]
    public async Task<string> SearchOfficialDocsAsync(
        [Description("The search query - be specific about the API, feature, or concept")] string query,
        [Description("Optional: technology filter (e.g., 'dotnet', 'aspnetcore', 'react', 'typescript')")] string? technology = null,
        CancellationToken cancellationToken = default)
    {
        var results = await _searchService.SearchAsync(
            query,
            source: ContentSource.OfficialDocs,
            technology: technology,
            limit: 5,
            minSimilarity: 0.65f,
            cancellationToken: cancellationToken);

        return FormatResults(results, "official documentation");
    }

    [KernelFunction("search_youtube")]
    [Description("Search YouTube video transcripts for practical tutorials, code walkthroughs, and implementation examples. Good for 'how to implement X' questions and seeing code in action.")]
    public async Task<string> SearchYouTubeAsync(
        [Description("The search query - describe the tutorial or implementation you're looking for")] string query,
        [Description("Optional: technology filter (e.g., 'dotnet', 'react')")] string? technology = null,
        CancellationToken cancellationToken = default)
    {
        var results = await _searchService.SearchAsync(
            query,
            source: ContentSource.YouTube,
            technology: technology,
            limit: 5,
            minSimilarity: 0.65f,
            cancellationToken: cancellationToken);

        return FormatResults(results, "YouTube tutorials");
    }

    [KernelFunction("search_articles")]
    [Description("Search expert blog articles for industry perspectives, deep dives, and practical insights. Good for understanding trade-offs, real-world experiences, and expert opinions on approaches.")]
    public async Task<string> SearchArticlesAsync(
        [Description("The search query - describe the topic or perspective you're looking for")] string query,
        [Description("Optional: filter by author name")] string? author = null,
        CancellationToken cancellationToken = default)
    {
        var results = await _searchService.SearchAsync(
            query,
            source: ContentSource.Article,
            author: author,
            limit: 5,
            minSimilarity: 0.65f,
            cancellationToken: cancellationToken);

        return FormatResults(results, "articles");
    }

    [KernelFunction("search_pulse")]
    [Description("Search Pulse community posts for real-world developer experiences, challenges, and solutions. Good for understanding how others have approached similar problems in practice.")]
    public async Task<string> SearchPulseAsync(
        [Description("The search query - describe the experience or challenge you want to find")] string query,
        CancellationToken cancellationToken = default)
    {
        var results = await _searchService.SearchAsync(
            query,
            source: ContentSource.Pulse,
            limit: 5,
            minSimilarity: 0.65f,
            cancellationToken: cancellationToken);

        return FormatResults(results, "Pulse community");
    }

    private static string FormatResults(IReadOnlyList<ContentSearchResult> results, string sourceName)
    {
        if (results.Count == 0)
        {
            return $"No relevant results found in {sourceName}.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Found {results.Count} relevant results from {sourceName}:");
        sb.AppendLine();

        var totalChars = 0;
        var maxChars = MaxResultTokens * CharsPerToken;

        foreach (var result in results)
        {
            // Build result entry
            var entry = new StringBuilder();
            entry.AppendLine($"### {result.Title}");

            if (!string.IsNullOrWhiteSpace(result.Author))
            {
                entry.AppendLine($"*By {result.Author}*");
            }

            entry.AppendLine();
            entry.AppendLine(result.Content);
            entry.AppendLine();
            entry.AppendLine("---");
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
                    sb.AppendLine($"### {result.Title}");
                    if (!string.IsNullOrWhiteSpace(result.Author))
                    {
                        sb.AppendLine($"*By {result.Author}*");
                    }
                    sb.AppendLine();
                    sb.AppendLine(truncatedContent);
                }
                break;
            }

            sb.Append(entryText);
            totalChars += entryText.Length;
        }

        return sb.ToString();
    }
}
