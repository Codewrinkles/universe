using Codewrinkles.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Codewrinkles.Infrastructure.Services;

public sealed class AvatarService : IAvatarService
{
    private const int AvatarSize = 500;
    private const int JpegQuality = 85;
    private const string AvatarsFolder = "avatars";

    private readonly string _avatarsPath;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AvatarService(
        IWebHostEnvironment environment,
        IHttpContextAccessor httpContextAccessor)
    {
        _avatarsPath = Path.Combine(environment.WebRootPath, AvatarsFolder);
        _httpContextAccessor = httpContextAccessor;

        // Ensure the avatars directory exists
        if (!Directory.Exists(_avatarsPath))
        {
            Directory.CreateDirectory(_avatarsPath);
        }
    }

    public async Task<string> SaveAvatarAsync(
        Stream imageStream,
        Guid profileId,
        CancellationToken cancellationToken)
    {
        // Load the image
        using var image = await Image.LoadAsync(imageStream, cancellationToken);

        // Resize to 500x500 with center crop
        image.Mutate(x => x
            .Resize(new ResizeOptions
            {
                Size = new Size(AvatarSize, AvatarSize),
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center
            }));

        // Generate filename
        var filename = $"{profileId}.jpg";
        var filePath = Path.Combine(_avatarsPath, filename);

        // Save as JPEG with specified quality
        var encoder = new JpegEncoder { Quality = JpegQuality };
        await image.SaveAsync(filePath, encoder, cancellationToken);

        // Build the full absolute URL
        var httpContext = _httpContextAccessor.HttpContext;
        var baseUrl = httpContext is not null
            ? $"{httpContext.Request.Scheme}://{httpContext.Request.Host}"
            : GetDefaultBaseUrl();

        return $"{baseUrl}/{AvatarsFolder}/{filename}";
    }

    private static string GetDefaultBaseUrl()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
            ? "https://localhost:7280"
            : "https://app-codwrinkles-api.azurewebsites.net";
    }

    public void DeleteAvatar(Guid profileId)
    {
        var filename = $"{profileId}.jpg";
        var filePath = Path.Combine(_avatarsPath, filename);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
