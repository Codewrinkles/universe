using Codewrinkles.Application;
using Codewrinkles.Infrastructure;
using Codewrinkles.Infrastructure.Options;
using Codewrinkles.API.Modules.Identity;
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
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add Application layer
builder.Services.AddApplication();

// Add Infrastructure layer
builder.Services.AddInfrastructure(builder.Configuration);

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
});

// Register authorization handlers
builder.Services.AddHttpContextAccessor(); // Required for handlers to access HttpContext
builder.Services.AddSingleton<IAuthorizationHandler, MustBeProfileOwnerHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, MustBePulseOwnerHandler>();

// Add exception handlers
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddProblemDetails();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173") // React frontend
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Add OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Codewrinkles.API"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource("Codewrinkles.*")
        .AddConsoleExporter()
    )
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter()
    );

// Add OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// Middleware pipeline
app.UseExceptionHandler();

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
app.MapPulseEndpoints();
app.MapSocialEndpoints();

app.Run();
