using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

public sealed record IngestDocsCommand(
    string HomepageUrl,
    string Technology,
    int MaxPages = 100
) : ICommand<IngestDocsResult>;

public sealed record IngestDocsResult(
    Guid JobId
);

public sealed class IngestDocsCommandHandler
    : ICommandHandler<IngestDocsCommand, IngestDocsResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IContentIngestionQueue _ingestionQueue;

    public IngestDocsCommandHandler(
        IUnitOfWork unitOfWork,
        IContentIngestionQueue ingestionQueue)
    {
        _unitOfWork = unitOfWork;
        _ingestionQueue = ingestionQueue;
    }

    public async Task<IngestDocsResult> HandleAsync(
        IngestDocsCommand command,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Nova.IngestDocs);

        try
        {
            // Create ingestion job
            var job = ContentIngestionJob.CreateForDocs(
                homepageUrl: command.HomepageUrl,
                technology: command.Technology,
                maxPages: command.MaxPages);

            _unitOfWork.ContentIngestionJobs.Create(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Queue for background processing
            await _ingestionQueue.QueueDocsScrapeAsync(
                job.Id,
                cancellationToken);

            activity?.SetSuccess(true);
            activity?.SetEntity("ContentIngestionJob", job.Id);
            activity?.SetTag("technology", command.Technology);
            activity?.SetTag("max_pages", command.MaxPages.ToString());

            return new IngestDocsResult(JobId: job.Id);
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}
