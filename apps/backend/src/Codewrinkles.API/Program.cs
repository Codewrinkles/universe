using Codewrinkles.Application;
using Codewrinkles.Infrastructure;
using Codewrinkles.Infrastructure.Options;
using Codewrinkles.API.Modules.Admin;
using Codewrinkles.API.Modules.Identity;
using Codewrinkles.API.Modules.Notification;
using Codewrinkles.API.Modules.Pulse;
using Codewrinkles.API.Modules.Social;
using Codewrinkles.API.ExceptionHandlers;
using Codewrinkles.API.Authorization.Requirements;
using Codewrinkles.API.Authorization.Handlers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Codewrinkles.Telemetry;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add Application layer
builder.Services.AddApplication();

// Add Infrastructure layer
builder.Services.AddInfrastructure(builder.Configuration);

// Add distributed cache for OAuth state management
builder.Services.AddDistributedMemoryCache();

// Add HttpClient for OAuthService
builder.Services.AddHttpClient<Codewrinkles.Application.Common.Interfaces.IOAuthService, Codewrinkles.Infrastructure.Services.OAuthService>();

// Add Authentication & Authorization
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ClockSkew = TimeSpan.Zero // Remove default 5-minute clock skew
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Policy: User must own the profile specified in route parameter
    options.AddPolicy("MustBeProfileOwner", policy =>
        policy.Requirements.Add(new MustBeProfileOwnerRequirement()));

    // Policy: User must own the pulse resource (used with IAuthorizationService)
    options.AddPolicy("MustBePulseOwner", policy =>
        policy.Requirements.Add(new MustBePulseOwnerRequirement()));

    // Policy: User must have Admin role
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});

// Register authorization handlers
builder.Services.AddHttpContextAccessor(); // Required for handlers to access HttpContext
builder.Services.AddSingleton<IAuthorizationHandler, MustBeProfileOwnerHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, MustBePulseOwnerHandler>();

// Add exception handlers (order matters - more specific handlers first)
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddExceptionHandler<InfrastructureExceptionHandler>();
builder.Services.AddExceptionHandler<CancellationExceptionHandler>();
builder.Services.AddProblemDetails();

// Add response compression (Brotli for modern browsers, Gzip for fallback)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true; // Enable compression for HTTPS
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest; // Balance speed vs size
});

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

// Add CORS - origins configured per environment in appsettings.json / appsettings.Development.json
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? throw new InvalidOperationException("CORS configuration is missing. Add Cors:AllowedOrigins to appsettings.");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromHours(24)); // Cache preflight responses for 24h
    });
});

// Add OpenTelemetry
// Development: Console only (zero Azure costs)
// Production: Azure Monitor only (no console noise)
var otelBuilder = builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: "Codewrinkles.API",
            serviceNamespace: "Codewrinkles",
            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0"))
    .WithTracing(tracing =>
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

        if (builder.Environment.IsDevelopment())
        {
            // Development: Console exporter only
            tracing.AddConsoleExporter();
        }
    })
    .WithMetrics(metrics =>
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

        if (builder.Environment.IsDevelopment())
        {
            // Development: Console exporter only
            metrics.AddConsoleExporter();
        }
    });

// Production: Add Azure Monitor exporter
// Connection string is read from APPLICATIONINSIGHTS_CONNECTION_STRING environment variable
if (!builder.Environment.IsDevelopment())
{
    otelBuilder.UseAzureMonitor(options =>
    {
        // 10% sampling to minimize costs (errors are always captured)
        options.SamplingRatio = 0.1f;
    });
}

// Add OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// Middleware pipeline
app.UseExceptionHandler();

// Response compression - must be before other middleware that writes to response
app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors();

// Authentication & Authorization (order matters!)
app.UseAuthentication();
app.UseAuthorization();

// Serve static files (for avatar images)
app.UseStaticFiles();

// Map endpoints
app.MapIdentityEndpoints();
app.MapProfileEndpoints();
app.MapPulseEndpoints();
app.MapBookmarkEndpoints();
app.MapSocialEndpoints();
app.MapNotificationEndpoints();
app.MapAdminEndpoints();

app.Run();
