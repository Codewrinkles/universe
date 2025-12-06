using System.Data;
using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Users;

public sealed record UploadAvatarCommand(
    Guid ProfileId,
    Stream ImageStream
) : ICommand<UploadAvatarResult>;

public sealed record UploadAvatarResult(
    Guid ProfileId,
    string AvatarUrl
);

public sealed class InvalidImageException : Exception
{
    public InvalidImageException(string message) : base(message) { }
}

public sealed class UploadAvatarCommandHandler
    : ICommandHandler<UploadAvatarCommand, UploadAvatarResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBlobStorageService _blobStorageService;

    public UploadAvatarCommandHandler(
        IUnitOfWork unitOfWork,
        IBlobStorageService blobStorageService)
    {
        _unitOfWork = unitOfWork;
        _blobStorageService = blobStorageService;
    }

    public async Task<UploadAvatarResult> HandleAsync(
        UploadAvatarCommand command,
        CancellationToken cancellationToken)
    {
        // Validator has already confirmed:
        // - Profile exists
        // - Image stream is valid

        string? blobUrl = null;

        try
        {
            // 1. Upload to blob storage FIRST (outside transaction)
            // Process and save the avatar image to Azure Blob Storage
            try
            {
                blobUrl = await _blobStorageService.UploadAvatarAsync(
                    command.ImageStream,
                    command.ProfileId,
                    cancellationToken);
            }
            catch (Exception ex) when (ex is not ProfileNotFoundException)
            {
                throw new InvalidImageException(
                    "Failed to process image. Please upload a valid image file (JPEG, PNG, GIF, or WebP).");
            }

            // 2. Update database within transaction
            // Isolation Level: ReadCommitted
            // - Prevents dirty reads (other transactions won't see profile with uncommitted avatar URL)
            // - No unnecessary locking (single entity update)
            await using var transaction = await _unitOfWork.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);
            try
            {
                // Find the profile (guaranteed to exist after validation)
                var profile = (await _unitOfWork.Profiles.FindByIdAsync(
                    command.ProfileId,
                    cancellationToken))!;

                // Update profile with blob URL (full absolute URL)
                profile.UpdateAvatarUrl(blobUrl);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Commit transaction - profile updated successfully
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                // Rollback database transaction on error
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch
        {
            // Clean up orphaned blob if it was uploaded but database update failed
            // This prevents accumulating orphaned blobs in storage
            if (blobUrl is not null)
            {
                try
                {
                    await _blobStorageService.DeleteAvatarAsync(command.ProfileId, cancellationToken);
                }
                catch
                {
                    // Log but don't throw - cleanup is best effort
                    // The orphaned blob can be cleaned up by a background job if needed
                }
            }

            throw;
        }

        // 3. Return result
        return new UploadAvatarResult(
            ProfileId: command.ProfileId,
            AvatarUrl: blobUrl!
        );
    }
}
