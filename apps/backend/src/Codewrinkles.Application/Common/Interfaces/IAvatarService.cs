namespace Codewrinkles.Application.Common.Interfaces;

public interface IAvatarService
{
    /// <summary>
    /// Process and save an avatar image.
    /// Resizes to 500x500 pixels and saves as JPEG.
    /// </summary>
    /// <param name="imageStream">The uploaded image stream</param>
    /// <param name="profileId">The profile ID to use as filename</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The relative URL path to the saved avatar</returns>
    Task<string> SaveAvatarAsync(
        Stream imageStream,
        Guid profileId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Delete an avatar image if it exists.
    /// </summary>
    /// <param name="profileId">The profile ID</param>
    void DeleteAvatar(Guid profileId);
}
