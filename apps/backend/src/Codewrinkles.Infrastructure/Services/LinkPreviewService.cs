using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Codewrinkles.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Codewrinkles.Infrastructure.Services;

public sealed class LinkPreviewService : ILinkPreviewService
{
    private static readonly Regex UrlRegex = new(
        @"https?://[^\s]+",
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
            // Validate URL
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return null;

            // Fetch HTML
            var response = await _httpClient.GetAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

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
