namespace Codewrinkles.Application.Common.Interfaces;

/// <summary>
/// Service for managing media uploads to Azure Blob Storage.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads an avatar image to blob storage.
    /// </summary>
    /// <param name="imageStream">The image stream to upload.</param>
    /// <param name="profileId">The profile ID (used as blob name).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The full blob URL where the avatar can be accessed.</returns>
    Task<string> UploadAvatarAsync(Stream imageStream, Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a pulse image to blob storage.
    /// </summary>
    /// <param name="imageStream">The image stream to upload.</param>
    /// <param name="pulseId">The pulse ID (used as blob name).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the blob URL, width, and height of the processed image.</returns>
    Task<(string Url, int Width, int Height)> UploadPulseImageAsync(Stream imageStream, Guid pulseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an avatar from blob storage.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAvatarAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a pulse image from blob storage.
    /// </summary>
    /// <param name="pulseId">The pulse ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeletePulseImageAsync(Guid pulseId, CancellationToken cancellationToken = default);
}
