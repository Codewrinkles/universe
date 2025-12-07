using Microsoft.Extensions.DependencyInjection;

namespace Codewrinkles.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register Kommand from this assembly
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(DependencyInjection).Assembly)
                  .WithValidation();
        });

        // Application services
        services.AddScoped<Pulse.PulseContentProcessor>();

        return services;
    }
}
