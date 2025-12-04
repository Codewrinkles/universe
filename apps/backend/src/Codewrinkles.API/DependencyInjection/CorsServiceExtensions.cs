namespace Codewrinkles.API.DependencyInjection;

public static class CorsServiceExtensions
{
    public static IServiceCollection AddCorsServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var corsOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? throw new InvalidOperationException(
                "CORS configuration is missing. Add Cors:AllowedOrigins to appsettings.");

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(corsOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .SetPreflightMaxAge(TimeSpan.FromHours(24));
            });
        });

        return services;
    }
}
