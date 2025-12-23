using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;
using System.Data;

namespace Codewrinkles.Application.Nova;

public sealed record DeleteIngestionJobCommand(
    Guid JobId
) : ICommand<DeleteIngestionJobResult>;

public sealed record DeleteIngestionJobResult(
    bool Success,
    string? Message
);

public sealed class DeleteIngestionJobCommandHandler
    : ICommandHandler<DeleteIngestionJobCommand, DeleteIngestionJobResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteIngestionJobCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteIngestionJobResult> HandleAsync(
        DeleteIngestionJobCommand command,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Nova.DeleteIngestionJob);

        try
        {
            var job = await _unitOfWork.ContentIngestionJobs.FindByIdAsync(
                command.JobId,
                cancellationToken);

            if (job is null)
            {
                return new DeleteIngestionJobResult(
                    Success: false,
                    Message: "Ingestion job not found");
            }

            // Don't allow deleting jobs that are currently processing
            if (job.Status == IngestionJobStatus.Processing)
            {
                return new DeleteIngestionJobResult(
                    Success: false,
                    Message: "Cannot delete a job that is currently processing");
            }

            // Use transaction to delete both job and associated chunks
            await using var transaction = await _unitOfWork.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

            try
            {
                // Delete associated content chunks first
                await _unitOfWork.ContentChunks.DeleteByParentDocumentIdAsync(
                    job.ParentDocumentId,
                    cancellationToken);

                // Delete the job
                _unitOfWork.ContentIngestionJobs.Delete(job);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                activity?.SetSuccess(true);
                activity?.SetEntity("ContentIngestionJob", command.JobId);

                return new DeleteIngestionJobResult(
                    Success: true,
                    Message: null);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}
