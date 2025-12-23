using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

public sealed record GetIngestionJobsQuery(
    IngestionJobStatus? StatusFilter = null
) : IQuery<GetIngestionJobsResult>;

public sealed record GetIngestionJobsResult(
    IReadOnlyList<IngestionJobDto> Jobs
);

public sealed record IngestionJobDto(
    Guid Id,
    string Source,
    string Status,
    string Title,
    string? Author,
    string? Technology,
    string? SourceUrl,
    int ChunksCreated,
    int? TotalPages,
    int? PagesProcessed,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt
);

public sealed class GetIngestionJobsQueryHandler
    : IQueryHandler<GetIngestionJobsQuery, GetIngestionJobsResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetIngestionJobsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetIngestionJobsResult> HandleAsync(
        GetIngestionJobsQuery query,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Nova.GetIngestionJobs);

        try
        {
            var jobs = await _unitOfWork.ContentIngestionJobs.GetAllAsync(
                query.StatusFilter,
                cancellationToken);

            var dtos = jobs
                .OrderByDescending(j => j.CreatedAt)
                .Select(j => new IngestionJobDto(
                    Id: j.Id,
                    Source: j.Source.ToString(),
                    Status: j.Status.ToString(),
                    Title: j.Title,
                    Author: j.Author,
                    Technology: j.Technology,
                    SourceUrl: j.SourceUrl,
                    ChunksCreated: j.ChunksCreated,
                    TotalPages: j.TotalPages,
                    PagesProcessed: j.PagesProcessed,
                    ErrorMessage: j.ErrorMessage,
                    CreatedAt: j.CreatedAt,
                    StartedAt: j.StartedAt,
                    CompletedAt: j.CompletedAt))
                .ToList();

            activity?.SetSuccess(true);

            return new GetIngestionJobsResult(Jobs: dtos);
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}
