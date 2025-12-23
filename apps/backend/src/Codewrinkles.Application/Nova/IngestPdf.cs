using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

public sealed record IngestPdfCommand(
    string Title,
    byte[] PdfBytes,
    string ContentType,          // "book" or "officialdocs"
    string? Author = null,       // Required for books
    string? Technology = null    // Required for official docs
) : ICommand<IngestPdfResult>;

public sealed record IngestPdfResult(
    Guid JobId
);

public sealed class IngestPdfCommandHandler
    : ICommandHandler<IngestPdfCommand, IngestPdfResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IContentIngestionQueue _ingestionQueue;

    public IngestPdfCommandHandler(
        IUnitOfWork unitOfWork,
        IContentIngestionQueue ingestionQueue)
    {
        _unitOfWork = unitOfWork;
        _ingestionQueue = ingestionQueue;
    }

    public async Task<IngestPdfResult> HandleAsync(
        IngestPdfCommand command,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Nova.IngestPdf);

        try
        {
            // Create ingestion job based on content type
            var job = command.ContentType.ToLowerInvariant() switch
            {
                "officialdocs" => ContentIngestionJob.CreateForPdfDocs(
                    title: command.Title,
                    technology: command.Technology ?? "general"),
                _ => ContentIngestionJob.CreateForPdf(
                    title: command.Title,
                    author: command.Author ?? "Unknown")
            };

            _unitOfWork.ContentIngestionJobs.Create(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Queue for background processing
            await _ingestionQueue.QueuePdfIngestionAsync(
                job.Id,
                command.PdfBytes,
                cancellationToken);

            activity?.SetSuccess(true);
            activity?.SetEntity("ContentIngestionJob", job.Id);

            return new IngestPdfResult(JobId: job.Id);
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}
