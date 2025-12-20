using Azure.Identity;
using Azure.Storage.Blobs;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Identity;
using Codewrinkles.Infrastructure.Configuration;
using Codewrinkles.Infrastructure.Email;
using Codewrinkles.Infrastructure.Options;
using Codewrinkles.Infrastructure.Persistence;
using Codewrinkles.Infrastructure.Persistence.Repositories;
using Codewrinkles.Infrastructure.Persistence.Repositories.Nova;
using Codewrinkles.Infrastructure.Services;
using Codewrinkles.Infrastructure.Services.Nova;
using Microsoft.SemanticKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;

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
        services.AddScoped<INovaRepository, NovaRepository>();
        services.AddScoped<INovaMemoryRepository, NovaMemoryRepository>();
        services.AddScoped<IAlphaApplicationRepository, AlphaApplicationRepository>();
        services.AddScoped<INovaMetricsRepository, NovaMetricsRepository>();

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
        // Simple, honest User-Agent - no browser impersonation
        services.AddHttpClient("LinkPreview", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add("User-Agent", "Codewrinkles/1.0");
        });
        services.AddScoped<ILinkPreviewService, LinkPreviewService>();

        // HttpClient for OAuthService
        services.AddHttpClient<IOAuthService, OAuthService>();

        // ===== Email Services =====
        //
        // Architecture: Channel (singleton queue) -> BackgroundService (singleton processor)
        //                                              -> creates scope per email
        //                                                  -> resolves IEmailSender (scoped)
        //                                                      -> uses ResendClient (via HttpClientFactory)
        //
        // IMPORTANT: Do NOT register IResend separately - AddHttpClient<ResendClient>() handles it.
        // Adding a separate registration causes conflicts and production failures.

        // 1. Configuration (singleton)
        var emailSection = configuration.GetSection(EmailSettings.SectionName);
        services.Configure<EmailSettings>(emailSection);

        // 2. Channel - singleton, thread-safe queue for background processing
        services.AddSingleton<EmailChannel>();

        // 3. Resend SDK - typed HttpClient registration
        //    This registers ResendClient with IHttpClientFactory management.
        //    ResendClient constructor takes: IOptionsSnapshot<ResendClientOptions>, HttpClient
        services.AddOptions();
        services.AddHttpClient<ResendClient>();
        services.Configure<ResendClientOptions>(o =>
        {
            o.ApiToken = configuration["Email:ApiKey"] ?? string.Empty;
        });

        // 4. Email sender - SCOPED (resolved inside scope created by background service)
        //    This ensures proper lifetime management with HttpClient and IOptionsSnapshot
        services.AddScoped<IEmailSender, ResendEmailSender>();

        // 5. Email queue interface - singleton, wraps the channel for handlers to enqueue
        services.AddSingleton<IEmailQueue, EmailQueue>();

        // 6. Repository for re-engagement queries
        services.AddScoped<IReengagementRepository, ReengagementRepository>();

        // 7. Repository for Pulse Alpha gamification
        services.AddScoped<IPulseAlphaGrantRepository, PulseAlphaGrantRepository>();

        // 8. Background services - singletons that use IServiceScopeFactory
        services.AddHostedService<EmailSenderBackgroundService>();
        services.AddHostedService<ReengagementBackgroundService>();
        services.AddHostedService<SevenDayWinbackBackgroundService>();
        services.AddHostedService<ThirtyDayWinbackBackgroundService>();
        services.AddHostedService<PulseAlphaGrantBackgroundService>();

        // ===== Nova AI Services =====
        //
        // Architecture: NovaSettings -> Semantic Kernel -> ILlmService
        //
        // IMPORTANT: OpenAI API key must be stored in User Secrets for local development.
        // In production, use Azure Key Vault or environment variables.

        // 1. Configuration
        var novaSection = configuration.GetSection(NovaSettings.SectionName);
        services.Configure<NovaSettings>(novaSection);

        // 2. Semantic Kernel with OpenAI (chat + embeddings)
        services.AddSingleton<Kernel>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<NovaSettings>>().Value;

            if (string.IsNullOrWhiteSpace(settings.OpenAIApiKey))
            {
                throw new InvalidOperationException(
                    "Nova:OpenAIApiKey is required. Store the API key in User Secrets for local development. " +
                    "Run: dotnet user-secrets set \"Nova:OpenAIApiKey\" \"your-api-key\"");
            }

            var builder = Kernel.CreateBuilder();

            // Chat completion for conversations
            builder.AddOpenAIChatCompletion(
                modelId: settings.ModelId,
                apiKey: settings.OpenAIApiKey);

            // Embedding generation for memory semantic search
            // SKEXP0010: AddOpenAIEmbeddingGenerator is experimental but recommended over deprecated ITextEmbeddingGenerationService
#pragma warning disable SKEXP0010
            builder.AddOpenAIEmbeddingGenerator(
                modelId: settings.EmbeddingModelId,
                apiKey: settings.OpenAIApiKey);
#pragma warning restore SKEXP0010

            return builder.Build();
        });

        // 3. LLM Service
        services.AddScoped<ILlmService, SemanticKernelLlmService>();

        // 4. Embedding Service for memory semantic search
        services.AddScoped<IEmbeddingService, OpenAIEmbeddingService>();

        return services;
    }
}
