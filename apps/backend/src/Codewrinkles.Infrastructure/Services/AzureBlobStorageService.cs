using Azure.Storage.Blobs;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace Codewrinkles.Infrastructure.Services;

public sealed class AzureBlobStorageService : IBlobStorageService
{
    private const int AvatarSize = 500;
    private const int MaxPulseImageDimension = 2048;
    private const int JpegQuality = 85;

    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobStorageSettings _settings;

    public AzureBlobStorageService(
        BlobServiceClient blobServiceClient,
        IOptions<BlobStorageSettings> settings)
    {
        _blobServiceClient = blobServiceClient;
        _settings = settings.Value;
    }

    public async Task<string> UploadAvatarAsync(
        Stream imageStream,
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        // Load and process the image
        using var image = await Image.LoadAsync(imageStream, cancellationToken);

        // Resize to 500x500 with center crop (same as AvatarService)
        image.Mutate(x => x
            .Resize(new ResizeOptions
            {
                Size = new Size(AvatarSize, AvatarSize),
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center
            }));

        // Generate blob name
        var blobName = $"{profileId}.jpg";

        // Upload to blob storage
        var containerClient = _blobServiceClient.GetBlobContainerClient(_settings.AvatarContainer);
        var blobClient = containerClient.GetBlobClient(blobName);

        // Save image to memory stream as JPEG
        using var outputStream = new MemoryStream();
        var encoder = new JpegEncoder { Quality = JpegQuality };
        await image.SaveAsync(outputStream, encoder, cancellationToken);
        outputStream.Position = 0;

        // Upload to blob storage with content type
        await blobClient.UploadAsync(
            outputStream,
            overwrite: true,
            cancellationToken: cancellationToken);

        // Set content type for proper browser rendering
        await blobClient.SetHttpHeadersAsync(
            new Azure.Storage.Blobs.Models.BlobHttpHeaders
            {
                ContentType = "image/jpeg"
            },
            cancellationToken: cancellationToken);

        // Return the full blob URL
        return blobClient.Uri.ToString();
    }

    public async Task<(string Url, int Width, int Height)> UploadPulseImageAsync(
        Stream imageStream,
        Guid pulseId,
        CancellationToken cancellationToken = default)
    {
        // Load the image
        using var image = await Image.LoadAsync(imageStream, cancellationToken);

        // Calculate new dimensions while maintaining aspect ratio (same as PulseImageService)
        var (newWidth, newHeight) = CalculateResizedDimensions(
            image.Width,
            image.Height,
            MaxPulseImageDimension);

        // Resize if needed (maintain aspect ratio, no cropping)
        if (newWidth != image.Width || newHeight != image.Height)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(newWidth, newHeight),
                Mode = ResizeMode.Max
            }));
        }

        // Generate blob name
        var blobName = $"{pulseId}.jpg";

        // Upload to blob storage
        var containerClient = _blobServiceClient.GetBlobContainerClient(_settings.PulseImageContainer);
        var blobClient = containerClient.GetBlobClient(blobName);

        // Save image to memory stream as JPEG
        using var outputStream = new MemoryStream();
        var encoder = new JpegEncoder { Quality = JpegQuality };
        await image.SaveAsync(outputStream, encoder, cancellationToken);
        outputStream.Position = 0;

        // Upload to blob storage with content type
        await blobClient.UploadAsync(
            outputStream,
            overwrite: true,
            cancellationToken: cancellationToken);

        // Set content type for proper browser rendering
        await blobClient.SetHttpHeadersAsync(
            new Azure.Storage.Blobs.Models.BlobHttpHeaders
            {
                ContentType = "image/jpeg"
            },
            cancellationToken: cancellationToken);

        // Return the full blob URL and dimensions
        return (blobClient.Uri.ToString(), image.Width, image.Height);
    }

    public async Task DeleteAvatarAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var blobName = $"{profileId}.jpg";
        var containerClient = _blobServiceClient.GetBlobContainerClient(_settings.AvatarContainer);
        var blobClient = containerClient.GetBlobClient(blobName);

        // DeleteIfExistsAsync is idempotent - won't throw if blob doesn't exist
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task DeletePulseImageAsync(Guid pulseId, CancellationToken cancellationToken = default)
    {
        var blobName = $"{pulseId}.jpg";
        var containerClient = _blobServiceClient.GetBlobContainerClient(_settings.PulseImageContainer);
        var blobClient = containerClient.GetBlobClient(blobName);

        // DeleteIfExistsAsync is idempotent - won't throw if blob doesn't exist
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    private static (int Width, int Height) CalculateResizedDimensions(
        int originalWidth,
        int originalHeight,
        int maxDimension)
    {
        // If image is already smaller than max, return original dimensions
        if (originalWidth <= maxDimension && originalHeight <= maxDimension)
        {
            return (originalWidth, originalHeight);
        }

        // Calculate aspect ratio
        var aspectRatio = (double)originalWidth / originalHeight;

        // Determine which dimension is larger and scale accordingly
        if (originalWidth > originalHeight)
        {
            // Width is larger - constrain by width
            var newWidth = maxDimension;
            var newHeight = (int)(maxDimension / aspectRatio);
            return (newWidth, newHeight);
        }
        else
        {
            // Height is larger or equal - constrain by height
            var newHeight = maxDimension;
            var newWidth = (int)(maxDimension * aspectRatio);
            return (newWidth, newHeight);
        }
    }
}
