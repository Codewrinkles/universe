using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Codewrinkles.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Codewrinkles.Infrastructure.Services;

public sealed class LinkPreviewService : ILinkPreviewService
{
    // Matches URLs but excludes trailing punctuation (.,;:!?)
    // Example: "Check this out https://example.com." extracts "https://example.com" (without the period)
    private static readonly Regex UrlRegex = new(
        @"https?://[^\s]+?(?=[.,;:!?)\]}\s]|$)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly HttpClient _httpClient;
    private readonly ILogger<LinkPreviewService> _logger;

    public LinkPreviewService(
        IHttpClientFactory httpClientFactory,
        ILogger<LinkPreviewService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("LinkPreview");
        _logger = logger;
    }

    public string? ExtractFirstUrl(string content)
    {
        var match = UrlRegex.Match(content);
        return match.Success ? match.Value : null;
    }

    public async Task<LinkPreviewData?> FetchPreviewAsync(
        string url,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching link preview for: {Url}", url);

            // Validate URL
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                _logger.LogWarning("Invalid URL format: {Url}", url);
                return null;
            }

            // Fetch HTML
            var response = await _httpClient.GetAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Link preview fetch failed for {Url} with HTTP {StatusCode}",
                    url,
                    (int)response.StatusCode);
                return null;
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);

            // Parse Open Graph tags
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var title = GetMetaContent(doc, "og:title")
                ?? doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim()
                ?? uri.Host;

            var description = GetMetaContent(doc, "og:description")
                ?? GetMetaContent(doc, "description");

            var imageUrl = GetMetaContent(doc, "og:image");

            // Decode HTML entities (e.g., &amp; → &, &quot; → ", &#39; → ')
            title = WebUtility.HtmlDecode(title);
            description = description is not null ? WebUtility.HtmlDecode(description) : null;

            return new LinkPreviewData(
                Url: url,
                Title: title,
                Domain: uri.Host,
                Description: description,
                ImageUrl: imageUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch link preview for URL: {Url}", url);
            return null;
        }
    }

    private static string? GetMetaContent(HtmlDocument doc, string property)
    {
        var node = doc.DocumentNode.SelectSingleNode($"//meta[@property='{property}']")
            ?? doc.DocumentNode.SelectSingleNode($"//meta[@name='{property}']");

        return node?.GetAttributeValue("content", null)?.Trim();
    }
}
