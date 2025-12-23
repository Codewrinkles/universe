using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

public sealed record IngestTranscriptCommand(
    string VideoId,
    string VideoUrl,
    string Title,
    string Transcript
) : ICommand<IngestTranscriptResult>;

public sealed record IngestTranscriptResult(
    Guid JobId
);

public sealed class IngestTranscriptCommandHandler
    : ICommandHandler<IngestTranscriptCommand, IngestTranscriptResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IContentIngestionQueue _ingestionQueue;

    public IngestTranscriptCommandHandler(
        IUnitOfWork unitOfWork,
        IContentIngestionQueue ingestionQueue)
    {
        _unitOfWork = unitOfWork;
        _ingestionQueue = ingestionQueue;
    }

    public async Task<IngestTranscriptResult> HandleAsync(
        IngestTranscriptCommand command,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Nova.IngestTranscript);

        try
        {
            // Create ingestion job
            var job = ContentIngestionJob.CreateForYouTube(
                videoId: command.VideoId,
                videoUrl: command.VideoUrl,
                title: command.Title);

            _unitOfWork.ContentIngestionJobs.Create(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Queue for background processing
            await _ingestionQueue.QueueTranscriptIngestionAsync(
                job.Id,
                command.Transcript,
                cancellationToken);

            activity?.SetSuccess(true);
            activity?.SetEntity("ContentIngestionJob", job.Id);

            return new IngestTranscriptResult(JobId: job.Id);
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}
