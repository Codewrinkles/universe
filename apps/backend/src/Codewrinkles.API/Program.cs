using Codewrinkles.Application;
using Codewrinkles.Infrastructure;
using Codewrinkles.API.DependencyInjection;
using Codewrinkles.API.Telemetry;
using Codewrinkles.API.Modules.Admin;
using Codewrinkles.API.Modules.Identity;
using Codewrinkles.API.Modules.Nova;
using Codewrinkles.API.Modules.Notification;
using Codewrinkles.API.Modules.Pulse;
using Codewrinkles.API.Modules.Social;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for large file uploads (PDFs up to 500MB for Azure DI S0)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 500 * 1024 * 1024; // 500MB
});

// Configure form options for multipart uploads
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 500 * 1024 * 1024; // 500MB
});

// CRITICAL: Enable DI validation in ALL environments (not just Development)
// This ensures scope violations and missing registrations fail at startup locally,
// not at runtime in production. Same behavior everywhere = no production surprises.
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

// Layer registration
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Distributed cache for OAuth state management
builder.Services.AddDistributedMemoryCache();

// Cross-cutting concerns
builder.Services.AddAuthServices(builder.Configuration);
builder.Services.AddExceptionHandling();
builder.Services.AddCompressionServices();
builder.Services.AddCorsServices(builder.Configuration);

// Telemetry with user identity enrichment
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<UserTelemetryProcessor>();
builder.Services.AddTelemetryServices(builder.Environment);

// OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// Middleware pipeline
app.UseExceptionHandler();
app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

// Endpoints
app.MapIdentityEndpoints();
app.MapProfileEndpoints();
app.MapPulseEndpoints();
app.MapBookmarkEndpoints();
app.MapSocialEndpoints();
app.MapNotificationEndpoints();
app.MapAdminEndpoints();
app.MapNovaEndpoints();
app.MapAlphaEndpoints();

app.Run();
