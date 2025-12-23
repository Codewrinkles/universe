using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

public sealed record IngestArticleCommand(
    string Title,
    string Content,
    string? Author = null,
    string? Url = null
) : ICommand<IngestArticleResult>;

public sealed record IngestArticleResult(
    Guid JobId
);

public sealed class IngestArticleCommandHandler
    : ICommandHandler<IngestArticleCommand, IngestArticleResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IContentIngestionQueue _ingestionQueue;

    public IngestArticleCommandHandler(
        IUnitOfWork unitOfWork,
        IContentIngestionQueue ingestionQueue)
    {
        _unitOfWork = unitOfWork;
        _ingestionQueue = ingestionQueue;
    }

    public async Task<IngestArticleResult> HandleAsync(
        IngestArticleCommand command,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Nova.IngestArticle);

        try
        {
            // Create ingestion job
            var job = ContentIngestionJob.CreateForArticle(
                title: command.Title,
                author: command.Author,
                url: command.Url);

            _unitOfWork.ContentIngestionJobs.Create(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Queue for background processing
            await _ingestionQueue.QueueArticleIngestionAsync(
                job.Id,
                command.Content,
                cancellationToken);

            activity?.SetSuccess(true);
            activity?.SetEntity("ContentIngestionJob", job.Id);

            return new IngestArticleResult(JobId: job.Id);
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}
