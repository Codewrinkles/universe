using Azure.Monitor.OpenTelemetry.AspNetCore;
using Codewrinkles.Telemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Codewrinkles.API.DependencyInjection;

public static class TelemetryServiceExtensions
{
    public static IServiceCollection AddTelemetryServices(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        var otelBuilder = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: "Codewrinkles.API",
                    serviceNamespace: "Codewrinkles",
                    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0"))
            .WithTracing(tracing => ConfigureTracing(tracing, environment))
            .WithMetrics(metrics => ConfigureMetrics(metrics, environment));

        // Production: Add Azure Monitor exporter
        // Connection string is read from APPLICATIONINSIGHTS_CONNECTION_STRING environment variable
        if (!environment.IsDevelopment())
        {
            otelBuilder.UseAzureMonitor(options =>
            {
                // 10% sampling to minimize costs (errors are always captured)
                options.SamplingRatio = 0.1f;
            });
        }

        return services;
    }

    private static void ConfigureTracing(TracerProviderBuilder tracing, IHostEnvironment environment)
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation();

        // Register custom ActivitySources from Telemetry project
        foreach (var sourceName in ActivitySources.AllSourceNames)
        {
            tracing.AddSource(sourceName);
        }

        if (environment.IsDevelopment())
        {
            tracing.AddConsoleExporter();
        }
    }

    private static void ConfigureMetrics(MeterProviderBuilder metrics, IHostEnvironment environment)
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();

        // Register custom Meters from Telemetry project
        foreach (var meterName in Meters.AllMeterNames)
        {
            metrics.AddMeter(meterName);
        }

        if (environment.IsDevelopment())
        {
            metrics.AddConsoleExporter();
        }
    }
}
