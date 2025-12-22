using Azure;
using Azure.AI.DocumentIntelligence;
using Codewrinkles.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace Codewrinkles.Infrastructure.Services.Nova;

/// <summary>
/// PDF extraction service using Azure Document Intelligence (formerly Form Recognizer).
/// Uses the prebuilt-read model for general document text extraction.
/// </summary>
public sealed class AzureDocumentIntelligencePdfExtractor : IPdfExtractorService
{
    private readonly DocumentIntelligenceClient _client;

    public AzureDocumentIntelligencePdfExtractor(IOptions<AzureDocumentIntelligenceSettings> settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Value.Endpoint))
        {
            throw new InvalidOperationException(
                "AzureDocumentIntelligence:Endpoint is required. " +
                "Store the endpoint in User Secrets for local development.");
        }

        if (string.IsNullOrWhiteSpace(settings.Value.ApiKey))
        {
            throw new InvalidOperationException(
                "AzureDocumentIntelligence:ApiKey is required. " +
                "Store the API key in User Secrets for local development.");
        }

        var endpoint = new Uri(settings.Value.Endpoint);
        var credential = new AzureKeyCredential(settings.Value.ApiKey);
        _client = new DocumentIntelligenceClient(endpoint, credential);
    }

    public async Task<IReadOnlyList<PdfPage>> ExtractPagesAsync(
        byte[] pdfBytes,
        CancellationToken cancellationToken = default)
    {
        // Use prebuilt-read model for general document text extraction
        var content = BinaryData.FromBytes(pdfBytes);

        var operation = await _client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            "prebuilt-read",
            content,
            cancellationToken: cancellationToken);

        var result = operation.Value;
        var pages = new List<PdfPage>();

        if (result.Pages is not null)
        {
            foreach (var page in result.Pages)
            {
                var pageContent = ExtractPageContent(result, page.PageNumber);
                pages.Add(new PdfPage(page.PageNumber, pageContent));
            }
        }

        return pages;
    }

    private static string ExtractPageContent(AnalyzeResult result, int pageNumber)
    {
        // Collect all text from paragraphs on this page
        var lines = new List<string>();

        if (result.Paragraphs is not null)
        {
            foreach (var paragraph in result.Paragraphs)
            {
                // Check if paragraph is on this page
                if (paragraph.BoundingRegions?.Any(r => r.PageNumber == pageNumber) == true)
                {
                    lines.Add(paragraph.Content);
                }
            }
        }

        return string.Join("\n\n", lines);
    }
}

/// <summary>
/// Configuration settings for Azure Document Intelligence.
/// </summary>
public sealed class AzureDocumentIntelligenceSettings
{
    public const string SectionName = "AzureDocumentIntelligence";

    /// <summary>
    /// The endpoint URL for Azure Document Intelligence.
    /// Example: https://your-resource.cognitiveservices.azure.com/
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// The API key for Azure Document Intelligence.
    /// Store in User Secrets for local development.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
