namespace Codewrinkles.Application.Common.Interfaces;

public interface ILinkPreviewService
{
    Task<LinkPreviewData?> FetchPreviewAsync(string url, CancellationToken cancellationToken);
    string? ExtractFirstUrl(string content);
}

public sealed record LinkPreviewData(
    string Url,
    string Title,
    string Domain,
    string? Description,
    string? ImageUrl);
