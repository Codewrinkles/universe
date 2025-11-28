using Codewrinkles.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Codewrinkles.Infrastructure.Services;

public sealed class PulseImageService : IPulseImageService
{
    private const int MaxImageDimension = 2048;
    private const int JpegQuality = 85;
    private const string PulseImagesFolder = "pulse-images";

    private readonly string _pulseImagesPath;

    public PulseImageService(IWebHostEnvironment environment)
    {
        _pulseImagesPath = Path.Combine(environment.WebRootPath, PulseImagesFolder);

        // Ensure the pulse-images directory exists
        if (!Directory.Exists(_pulseImagesPath))
        {
            Directory.CreateDirectory(_pulseImagesPath);
        }
    }

    public async Task<(string Url, int Width, int Height)> SavePulseImageAsync(
        Stream imageStream,
        Guid pulseId,
        CancellationToken cancellationToken)
    {
        // Load the image
        using var image = await Image.LoadAsync(imageStream, cancellationToken);

        // Calculate new dimensions while maintaining aspect ratio
        var (newWidth, newHeight) = CalculateResizedDimensions(
            image.Width,
            image.Height,
            MaxImageDimension);

        // Resize if needed (maintain aspect ratio, no cropping)
        if (newWidth != image.Width || newHeight != image.Height)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(newWidth, newHeight),
                Mode = ResizeMode.Max
            }));
        }

        // Generate filename
        var filename = $"{pulseId}.jpg";
        var filePath = Path.Combine(_pulseImagesPath, filename);

        // Save as JPEG with specified quality
        var encoder = new JpegEncoder { Quality = JpegQuality };
        await image.SaveAsync(filePath, encoder, cancellationToken);

        // Return the relative URL path and dimensions
        var url = $"/{PulseImagesFolder}/{filename}";
        return (url, image.Width, image.Height);
    }

    public void DeletePulseImage(Guid pulseId)
    {
        var filename = $"{pulseId}.jpg";
        var filePath = Path.Combine(_pulseImagesPath, filename);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
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
