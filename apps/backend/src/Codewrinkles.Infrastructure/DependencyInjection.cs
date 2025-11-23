using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Codewrinkles.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database (uncomment when ApplicationDbContext is created)
        // services.AddDbContext<ApplicationDbContext>(options =>
        // {
        //     options.UseSqlServer(
        //         configuration.GetConnectionString("DefaultConnection"),
        //         b => b.MigrationsAssembly(typeof(DependencyInjection).Assembly.FullName));
        // });

        return services;
    }
}
