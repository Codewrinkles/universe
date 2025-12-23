using System.Data;
using System.Net;
using System.Text.RegularExpressions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Text;

namespace Codewrinkles.Infrastructure.Services.Nova;

/// <summary>
/// Background service that consumes content ingestion jobs from the channel.
/// </summary>
public sealed partial class ContentIngestionBackgroundService : BackgroundService
{
    private readonly ContentIngestionChannel _channel;
    private readonly ContentEmbeddingCache _embeddingCache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ContentIngestionBackgroundService> _logger;

    public ContentIngestionBackgroundService(
        ContentIngestionChannel channel,
        ContentEmbeddingCache embeddingCache,
        IServiceScopeFactory scopeFactory,
        ILogger<ContentIngestionBackgroundService> logger)
    {
        _channel = channel;
        _embeddingCache = embeddingCache;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Content ingestion background service started");

        await foreach (var message in _channel.ReadAllAsync(stoppingToken))
        {
            try
            {
                // Create a scope per job - ensures proper lifetime for scoped services
                // Must use CreateAsyncScope() because UnitOfWork implements IAsyncDisposable
                await using var scope = _scopeFactory.CreateAsyncScope();

                await ProcessMessageAsync(message, scope.ServiceProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ingestion message for job {JobId}", message.JobId);
                // Log and continue - never crash the service
            }
        }

        _logger.LogInformation("Content ingestion background service stopped");
    }

    private async Task ProcessMessageAsync(
        ContentIngestionMessage message,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        switch (message)
        {
            case PdfIngestionMessage pdf:
                await ProcessPdfIngestionAsync(pdf.JobId, pdf.PdfBytes, services, cancellationToken);
                break;
            case TranscriptIngestionMessage transcript:
                await ProcessTranscriptIngestionAsync(transcript.JobId, transcript.Transcript, services, cancellationToken);
                break;
            case DocsScrapeMessage docs:
                await ProcessDocsScrapeAsync(docs.JobId, services, cancellationToken);
                break;
            case ArticleIngestionMessage article:
                await ProcessArticleIngestionAsync(article.JobId, article.Content, services, cancellationToken);
                break;
        }
    }

    private async Task ProcessPdfIngestionAsync(
        Guid jobId,
        byte[] pdfBytes,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        var embeddingService = services.GetRequiredService<IEmbeddingService>();
        var pdfExtractor = services.GetRequiredService<IPdfExtractorService>();

        var job = await unitOfWork.ContentIngestionJobs.FindByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            _logger.LogWarning("Job {JobId} not found", jobId);
            return;
        }

        job.MarkAsProcessing();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await using var transaction = await unitOfWork.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        try
        {
            // 1. Extract text from PDF (Azure Document Intelligence)
            var pages = await pdfExtractor.ExtractPagesAsync(pdfBytes, cancellationToken);
            job.UpdateProgress(0, pages.Count);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // 2. Chunk and embed each page
            var chunkCount = 0;
            for (var i = 0; i < pages.Count; i++)
            {
                var pageContent = pages[i].Content;
                var pageChunks = TextChunker.SplitPlainTextParagraphs(
                    TextChunker.SplitPlainTextLines(pageContent, maxTokensPerLine: 100),
                    maxTokensPerParagraph: 400,
                    overlapTokens: 50);

                foreach (var chunkContent in pageChunks)
                {
                    if (string.IsNullOrWhiteSpace(chunkContent)) continue;

                    var embedding = await embeddingService.GetEmbeddingAsync(chunkContent, cancellationToken);
                    var embeddingBytes = embeddingService.SerializeEmbedding(embedding);
                    var tokenCount = EstimateTokens(chunkContent);

                    var chunk = ContentChunk.Create(
                        source: job.Source,
                        sourceIdentifier: $"{job.ParentDocumentId}_{chunkCount}",
                        title: $"{job.Title} - Page {i + 1}",
                        content: chunkContent,
                        embedding: embeddingBytes,
                        tokenCount: tokenCount,
                        author: job.Author,
                        technology: job.Technology,
                        parentDocumentId: job.ParentDocumentId,
                        chunkIndex: chunkCount);

                    unitOfWork.ContentChunks.Create(chunk);
                    chunkCount++;
                }

                // Update progress after each page
                job.UpdateProgress(i + 1, pages.Count);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 3. Mark job complete and commit
            job.MarkAsCompleted(chunkCount);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // 4. Refresh embedding cache so new chunks are available for RAG
            await _embeddingCache.RefreshAsync(cancellationToken);

            _logger.LogInformation(
                "Completed PDF ingestion for job {JobId}: {ChunkCount} chunks created",
                jobId, chunkCount);
        }
        catch (Exception ex)
        {
            // Rollback all database changes
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(ex, "Failed to process PDF for job {JobId}", jobId);

            // Update job status (separate operation, not in rolled-back transaction)
            job.MarkAsFailed(ex.Message);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ProcessTranscriptIngestionAsync(
        Guid jobId,
        string transcript,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        var embeddingService = services.GetRequiredService<IEmbeddingService>();

        var job = await unitOfWork.ContentIngestionJobs.FindByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            _logger.LogWarning("Job {JobId} not found", jobId);
            return;
        }

        job.MarkAsProcessing();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await using var transaction = await unitOfWork.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        try
        {
            // Clean transcript: remove timestamps, filler words, section markers
            var cleanedTranscript = TranscriptCleaner.Clean(transcript);

            _logger.LogInformation(
                "Cleaned transcript for job {JobId}: {OriginalLength} -> {CleanedLength} chars ({Reduction:P0} reduction)",
                jobId,
                transcript.Length,
                cleanedTranscript.Length,
                1 - (double)cleanedTranscript.Length / transcript.Length);

            // Chunk transcript using SK TextChunker
            var chunks = TextChunker.SplitPlainTextParagraphs(
                TextChunker.SplitPlainTextLines(cleanedTranscript, maxTokensPerLine: 100),
                maxTokensPerParagraph: 400,
                overlapTokens: 50);

            var chunkCount = 0;
            foreach (var chunkContent in chunks)
            {
                if (string.IsNullOrWhiteSpace(chunkContent)) continue;

                var embedding = await embeddingService.GetEmbeddingAsync(chunkContent, cancellationToken);
                var embeddingBytes = embeddingService.SerializeEmbedding(embedding);
                var tokenCount = EstimateTokens(chunkContent);

                var chunk = ContentChunk.Create(
                    source: ContentSource.YouTube,
                    sourceIdentifier: $"{job.ParentDocumentId}_{chunkCount}",
                    title: $"{job.Title} (Part {chunkCount + 1})",
                    content: chunkContent,
                    embedding: embeddingBytes,
                    tokenCount: tokenCount,
                    parentDocumentId: job.ParentDocumentId,
                    chunkIndex: chunkCount);

                unitOfWork.ContentChunks.Create(chunk);
                chunkCount++;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            job.MarkAsCompleted(chunkCount);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Refresh embedding cache so new chunks are available for RAG
            await _embeddingCache.RefreshAsync(cancellationToken);

            _logger.LogInformation(
                "Completed transcript ingestion for job {JobId}: {ChunkCount} chunks created",
                jobId, chunkCount);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(ex, "Failed to process transcript for job {JobId}", jobId);

            job.MarkAsFailed(ex.Message);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ProcessDocsScrapeAsync(
        Guid jobId,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        var embeddingService = services.GetRequiredService<IEmbeddingService>();
        var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();

        var job = await unitOfWork.ContentIngestionJobs.FindByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            _logger.LogWarning("Job {JobId} not found", jobId);
            return;
        }

        if (string.IsNullOrEmpty(job.SourceUrl))
        {
            job.MarkAsFailed("No source URL specified");
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        job.MarkAsProcessing();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await using var transaction = await unitOfWork.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        try
        {
            var httpClient = httpClientFactory.CreateClient("DocsScraper");
            var homepageUri = new Uri(job.SourceUrl);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var toVisit = new Queue<string>();
            toVisit.Enqueue(job.SourceUrl);

            var chunkCount = 0;
            var pagesProcessed = 0;
            var maxPages = job.MaxPages ?? 100;

            while (toVisit.Count > 0 && pagesProcessed < maxPages)
            {
                var url = toVisit.Dequeue();
                if (visited.Contains(url)) continue;
                visited.Add(url);

                try
                {
                    // Rate limiting: 1 request per second
                    await Task.Delay(1000, cancellationToken);

                    var html = await httpClient.GetStringAsync(url, cancellationToken);
                    var markdown = ConvertHtmlToMarkdown(html);

                    // Extract links for crawling
                    var links = ExtractInternalLinks(html, homepageUri);
                    foreach (var link in links)
                    {
                        if (!visited.Contains(link))
                        {
                            toVisit.Enqueue(link);
                        }
                    }

                    // Chunk the markdown content
                    var chunks = TextChunker.SplitMarkdownParagraphs(
                        TextChunker.SplitMarkDownLines(markdown, maxTokensPerLine: 100),
                        maxTokensPerParagraph: 400,
                        overlapTokens: 50);

                    foreach (var chunkContent in chunks)
                    {
                        if (string.IsNullOrWhiteSpace(chunkContent)) continue;

                        var embedding = await embeddingService.GetEmbeddingAsync(
                            chunkContent, cancellationToken);
                        var embeddingBytes = embeddingService.SerializeEmbedding(embedding);
                        var tokenCount = EstimateTokens(chunkContent);

                        var chunk = ContentChunk.Create(
                            source: ContentSource.OfficialDocs,
                            sourceIdentifier: $"{job.ParentDocumentId}_{chunkCount}",
                            title: ExtractPageTitle(html) ?? $"Page {pagesProcessed + 1}",
                            content: chunkContent,
                            embedding: embeddingBytes,
                            tokenCount: tokenCount,
                            technology: job.Technology,
                            parentDocumentId: job.ParentDocumentId,
                            chunkIndex: chunkCount);

                        unitOfWork.ContentChunks.Create(chunk);
                        chunkCount++;
                    }

                    pagesProcessed++;
                    job.UpdateProgress(pagesProcessed, Math.Min(visited.Count + toVisit.Count, maxPages));
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch {Url}", url);
                    // Continue with other pages
                }
            }

            job.MarkAsCompleted(chunkCount);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Refresh embedding cache so new chunks are available for RAG
            await _embeddingCache.RefreshAsync(cancellationToken);

            _logger.LogInformation(
                "Completed docs scrape for job {JobId}: {ChunkCount} chunks from {PageCount} pages",
                jobId, chunkCount, pagesProcessed);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(ex, "Failed to scrape docs for job {JobId}", jobId);

            job.MarkAsFailed(ex.Message);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ProcessArticleIngestionAsync(
        Guid jobId,
        string content,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        var embeddingService = services.GetRequiredService<IEmbeddingService>();

        var job = await unitOfWork.ContentIngestionJobs.FindByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            _logger.LogWarning("Job {JobId} not found", jobId);
            return;
        }

        job.MarkAsProcessing();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await using var transaction = await unitOfWork.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        try
        {
            // Chunk article using SK TextChunker
            var chunks = TextChunker.SplitPlainTextParagraphs(
                TextChunker.SplitPlainTextLines(content, maxTokensPerLine: 100),
                maxTokensPerParagraph: 400,
                overlapTokens: 50);

            var chunkCount = 0;
            foreach (var chunkContent in chunks)
            {
                if (string.IsNullOrWhiteSpace(chunkContent)) continue;

                var embedding = await embeddingService.GetEmbeddingAsync(chunkContent, cancellationToken);
                var embeddingBytes = embeddingService.SerializeEmbedding(embedding);
                var tokenCount = EstimateTokens(chunkContent);

                var chunk = ContentChunk.Create(
                    source: ContentSource.Article,
                    sourceIdentifier: job.SourceUrl ?? $"{job.ParentDocumentId}_{chunkCount}",
                    title: $"{job.Title} (Part {chunkCount + 1})",
                    content: chunkContent,
                    embedding: embeddingBytes,
                    tokenCount: tokenCount,
                    author: job.Author,
                    parentDocumentId: job.ParentDocumentId,
                    chunkIndex: chunkCount);

                unitOfWork.ContentChunks.Create(chunk);
                chunkCount++;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            job.MarkAsCompleted(chunkCount);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Refresh embedding cache so new chunks are available for RAG
            await _embeddingCache.RefreshAsync(cancellationToken);

            _logger.LogInformation(
                "Completed article ingestion for job {JobId}: {ChunkCount} chunks created",
                jobId, chunkCount);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(ex, "Failed to process article for job {JobId}", jobId);

            job.MarkAsFailed(ex.Message);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    // Helper methods
    private static int EstimateTokens(string text) => text.Length / 4;

    private static string ConvertHtmlToMarkdown(string html)
    {
        // Basic HTML to Markdown conversion
        // Strip <script>, <style>, <nav>, <footer>, <header> tags first
        var cleaned = ScriptStyleNavFooterHeaderRegex().Replace(html, "");

        // Strip remaining HTML tags
        cleaned = HtmlTagRegex().Replace(cleaned, " ");

        // Normalize whitespace
        cleaned = WhitespaceRegex().Replace(cleaned, " ");

        return WebUtility.HtmlDecode(cleaned.Trim());
    }

    private static IEnumerable<string> ExtractInternalLinks(string html, Uri baseUri)
    {
        var matches = HrefRegex().Matches(html);

        foreach (Match match in matches)
        {
            var href = match.Groups[1].Value;
            if (Uri.TryCreate(baseUri, href, out var absoluteUri))
            {
                // Only internal links under the same path
                if (absoluteUri.Host == baseUri.Host &&
                    absoluteUri.AbsolutePath.StartsWith(baseUri.AbsolutePath))
                {
                    // Remove fragment and query
                    var cleanUrl = $"{absoluteUri.Scheme}://{absoluteUri.Host}{absoluteUri.AbsolutePath}";
                    yield return cleanUrl;
                }
            }
        }
    }

    private static string? ExtractPageTitle(string html)
    {
        var match = TitleRegex().Match(html);
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value.Trim()) : null;
    }

    // Compiled regex patterns for better performance
    [GeneratedRegex(@"<(script|style|nav|footer|header|aside)[^>]*>[\s\S]*?</\1>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptStyleNavFooterHeaderRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"href=[""']([^""']+)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex HrefRegex();

    [GeneratedRegex(@"<title[^>]*>([^<]+)</title>", RegexOptions.IgnoreCase)]
    private static partial Regex TitleRegex();
}
