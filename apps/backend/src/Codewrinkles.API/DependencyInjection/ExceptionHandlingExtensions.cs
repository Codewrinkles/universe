using Codewrinkles.API.ExceptionHandlers;

namespace Codewrinkles.API.DependencyInjection;

public static class ExceptionHandlingExtensions
{
    public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
    {
        // Order matters - more specific handlers first
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<DomainExceptionHandler>();
        services.AddExceptionHandler<InfrastructureExceptionHandler>();
        services.AddExceptionHandler<CancellationExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
