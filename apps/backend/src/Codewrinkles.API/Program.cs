using Codewrinkles.Application;
using Codewrinkles.Infrastructure;
using Codewrinkles.API.DependencyInjection;
using Codewrinkles.API.Modules.Admin;
using Codewrinkles.API.Modules.Identity;
using Codewrinkles.API.Modules.Notification;
using Codewrinkles.API.Modules.Pulse;
using Codewrinkles.API.Modules.Social;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

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

app.Run();
