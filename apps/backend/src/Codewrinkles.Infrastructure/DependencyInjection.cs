using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Identity;
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
        // Database
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            var env = serviceProvider.GetService<IHostEnvironment>();
            var isDevelopment = env?.IsDevelopment() ?? false;

            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(DependencyInjection).Assembly.FullName));

            // Enable detailed SQL logging in development - TEMPORARILY DISABLED
            // if (isDevelopment)
            // {
            //     options.EnableSensitiveDataLogging(); // Show parameter values
            //     options.EnableDetailedErrors();       // Show detailed error messages
            //     options.LogTo(Console.WriteLine, LogLevel.Information); // Log all SQL commands with execution time
            // }
        });

        // Repositories
        services.AddScoped<IIdentityRepository, IdentityRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IPulseRepository, PulseRepository>();
        services.AddScoped<IFollowRepository, FollowRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IBookmarkRepository, BookmarkRepository>();

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
                AccessTokenExpiryMinutes: jwtOptions.AccessTokenExpiryMinutes
            );
            return new JwtTokenGenerator(jwtSettings);
        });

        // Application Services
        services.AddScoped<IAvatarService, AvatarService>();
        services.AddScoped<IPulseImageService, PulseImageService>();

        return services;
    }
}
