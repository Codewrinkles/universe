using Azure.Identity;
using Azure.Storage.Blobs;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Identity;
using Codewrinkles.Infrastructure.Configuration;
using Codewrinkles.Infrastructure.Options;
using Codewrinkles.Infrastructure.Persistence;
using Codewrinkles.Infrastructure.Persistence.Repositories;
using Codewrinkles.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Codewrinkles.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // IMPORTANT: Using BOTH factory (for parallel queries) and scoped DbContext (for normal DI)
        // Only configure the FACTORY - DbContext will use the factory's configuration

        // DbContext Factory - For parallel query operations in repositories
        // This is the ONLY place we configure the database connection
        services.AddDbContextFactory<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(DependencyInjection).Assembly.FullName));
        });

        // Database - Scoped DbContext for regular operations
        // NO OPTIONS CONFIGURATION - uses the factory's configuration automatically
        services.AddDbContext<ApplicationDbContext>();

        // Repositories
        services.AddScoped<IIdentityRepository, IdentityRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IExternalLoginRepository, ExternalLoginRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPulseRepository, PulseRepository>();
        services.AddScoped<IFollowRepository, FollowRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IBookmarkRepository, BookmarkRepository>();
        services.AddScoped<IHashtagRepository, HashtagRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // JWT Options (from configuration)
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        services.Configure<JwtOptions>(options => jwtSection.Bind(options));

        // Domain Services
        services.AddScoped<PasswordHasher>();
        services.AddScoped<JwtTokenGenerator>(sp =>
        {
            var jwtOptions = sp.GetRequiredService<IOptions<JwtOptions>>().Value;
            var jwtSettings = new JwtSettings(
                SecretKey: jwtOptions.SecretKey,
                Issuer: jwtOptions.Issuer,
                Audience: jwtOptions.Audience,
                AccessTokenExpiryMinutes: jwtOptions.AccessTokenExpiryMinutes,
                RefreshTokenExpiryDays: jwtOptions.RefreshTokenExpiryDays
            );
            return new JwtTokenGenerator(jwtSettings);
        });

        // Blob Storage Configuration
        var blobStorageSection = configuration.GetSection(BlobStorageSettings.SectionName);
        services.Configure<BlobStorageSettings>(blobStorageSection);

        // BlobServiceClient - Singleton for connection pooling
        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<BlobStorageSettings>>().Value;

            if (settings.UseManagedIdentity)
            {
                // Production: Use Managed Identity (no credentials needed)
                var blobUri = new Uri(settings.GetBlobStorageUrl());
                return new BlobServiceClient(blobUri, new DefaultAzureCredential());
            }
            else
            {
                // Local development: Use connection string from User Secrets
                if (string.IsNullOrWhiteSpace(settings.ConnectionString))
                {
                    throw new InvalidOperationException(
                        "BlobStorage:ConnectionString is required when UseManagedIdentity is false. " +
                        "Store the connection string in User Secrets for local development.");
                }
                return new BlobServiceClient(settings.ConnectionString);
            }
        });

        // Blob Storage Service
        services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();

        // HttpClient for LinkPreviewService
        // Configured to mimic a real Chrome browser to avoid bot detection
        // Modern sites check Sec-Fetch-* headers - bots that don't send these get blocked/served generic HTML
        services.AddHttpClient("LinkPreview", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);

            // Use TryAddWithoutValidation for better header compatibility
            // Chrome 120+ headers (Dec 2024)
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br");

            // CRITICAL: Sec-Fetch headers - sites use these to detect bots
            // Real browsers always send these, bots typically don't
            client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Dest", "document");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Mode", "navigate");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Site", "none");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-User", "?1");

            // Client Hints headers - must match User-Agent
            client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Ch-Ua",
                "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"120\", \"Google Chrome\";v=\"120\"");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Ch-Ua-Mobile", "?0");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Ch-Ua-Platform", "\"Windows\"");

            client.DefaultRequestHeaders.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Cache-Control", "max-age=0");
        });
        services.AddScoped<ILinkPreviewService, LinkPreviewService>();

        // HttpClient for OAuthService
        services.AddHttpClient<IOAuthService, OAuthService>();

        return services;
    }
}
