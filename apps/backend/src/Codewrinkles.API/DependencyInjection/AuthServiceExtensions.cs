using System.Text;
using Codewrinkles.API.Authorization.Handlers;
using Codewrinkles.API.Authorization.Requirements;
using Codewrinkles.Infrastructure.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Codewrinkles.API.DependencyInjection;

public static class AuthServiceExtensions
{
    public static IServiceCollection AddAuthServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is missing");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("MustBeProfileOwner", policy =>
                policy.Requirements.Add(new MustBeProfileOwnerRequirement()));

            options.AddPolicy("MustBePulseOwner", policy =>
                policy.Requirements.Add(new MustBePulseOwnerRequirement()));

            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy("RequiresNovaAccess", policy =>
                policy.Requirements.Add(new RequiresNovaAccessRequirement()));
        });

        services.AddHttpContextAccessor();
        services.AddSingleton<IAuthorizationHandler, MustBeProfileOwnerHandler>();
        services.AddSingleton<IAuthorizationHandler, MustBePulseOwnerHandler>();
        services.AddScoped<IAuthorizationHandler, RequiresNovaAccessHandler>();

        return services;
    }
}
