namespace Codewrinkles.Application.Common.Interfaces;

public interface IPulseImageService
{
    /// <summary>
    /// Process and save a pulse image.
    /// Resizes to max 2048px while maintaining aspect ratio and saves as JPEG.
    /// </summary>
    /// <param name="imageStream">The uploaded image stream</param>
    /// <param name="pulseId">The pulse ID to use as filename</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A tuple containing the relative URL path, width, and height of the saved image</returns>
    Task<(string Url, int Width, int Height)> SavePulseImageAsync(
        Stream imageStream,
        Guid pulseId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Delete a pulse image if it exists.
    /// </summary>
    /// <param name="pulseId">The pulse ID</param>
    void DeletePulseImage(Guid pulseId);
}
