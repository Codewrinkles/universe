using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

public sealed record GetIngestionJobQuery(
    Guid JobId
) : IQuery<GetIngestionJobResult>;

public sealed record GetIngestionJobResult(
    IngestionJobDto? Job
);

public sealed class GetIngestionJobQueryHandler
    : IQueryHandler<GetIngestionJobQuery, GetIngestionJobResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetIngestionJobQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetIngestionJobResult> HandleAsync(
        GetIngestionJobQuery query,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Nova.GetIngestionJob);

        try
        {
            var job = await _unitOfWork.ContentIngestionJobs.FindByIdAsync(
                query.JobId,
                cancellationToken);

            if (job is null)
            {
                return new GetIngestionJobResult(Job: null);
            }

            var dto = new IngestionJobDto(
                Id: job.Id,
                Source: job.Source.ToString(),
                Status: job.Status.ToString(),
                Title: job.Title,
                Author: job.Author,
                Technology: job.Technology,
                SourceUrl: job.SourceUrl,
                ChunksCreated: job.ChunksCreated,
                TotalPages: job.TotalPages,
                PagesProcessed: job.PagesProcessed,
                ErrorMessage: job.ErrorMessage,
                CreatedAt: job.CreatedAt,
                StartedAt: job.StartedAt,
                CompletedAt: job.CompletedAt);

            activity?.SetSuccess(true);

            return new GetIngestionJobResult(Job: dto);
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}
