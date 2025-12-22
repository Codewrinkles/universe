namespace Codewrinkles.Application.Common.Interfaces;

/// <summary>
/// Service for extracting text from PDF documents.
/// </summary>
public interface IPdfExtractorService
{
    /// <summary>
    /// Extract text content from each page of a PDF.
    /// </summary>
    Task<IReadOnlyList<PdfPage>> ExtractPagesAsync(
        byte[] pdfBytes,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a page extracted from a PDF.
/// </summary>
public sealed record PdfPage(int PageNumber, string Content);
