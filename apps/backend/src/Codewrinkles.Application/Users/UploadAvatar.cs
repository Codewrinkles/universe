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
    private readonly IAvatarService _avatarService;

    public UploadAvatarCommandHandler(
        IUnitOfWork unitOfWork,
        IAvatarService avatarService)
    {
        _unitOfWork = unitOfWork;
        _avatarService = avatarService;
    }

    public async Task<UploadAvatarResult> HandleAsync(
        UploadAvatarCommand command,
        CancellationToken cancellationToken)
    {
        // Validator has already confirmed:
        // - Profile exists
        // - Image stream is valid

        string? avatarUrl = null;

        // Perform all operations atomically within a transaction
        // Isolation Level: ReadCommitted
        // - Prevents dirty reads (other transactions won't see profile with uncommitted avatar URL)
        // - No unnecessary locking (single entity update)
        await using var transaction = await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        try
        {
            // 1. Find the profile (guaranteed to exist after validation)
            var profile = (await _unitOfWork.Profiles.FindByIdAsync(
                command.ProfileId,
                cancellationToken))!;

            // 2. Process and save the avatar image to disk
            // This is a file I/O operation - if it fails, we'll rollback and clean up
            try
            {
                avatarUrl = await _avatarService.SaveAvatarAsync(
                    command.ImageStream,
                    command.ProfileId,
                    cancellationToken);
            }
            catch (Exception ex) when (ex is not ProfileNotFoundException)
            {
                throw new InvalidImageException(
                    "Failed to process image. Please upload a valid image file (JPEG, PNG, GIF, or WebP).");
            }

            // 3. Update profile with new avatar URL
            profile.UpdateAvatarUrl(avatarUrl);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Commit transaction - profile updated successfully
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            // Rollback on any error - no database changes persisted
            await transaction.RollbackAsync(cancellationToken);

            // Clean up orphaned avatar file if it was saved to disk
            // The file I/O isn't part of the database transaction, so we must clean up manually
            if (avatarUrl is not null)
            {
                _avatarService.DeleteAvatar(command.ProfileId);
            }

            throw;
        }

        // 4. Return result
        return new UploadAvatarResult(
            ProfileId: command.ProfileId,
            AvatarUrl: avatarUrl!
        );
    }
}
