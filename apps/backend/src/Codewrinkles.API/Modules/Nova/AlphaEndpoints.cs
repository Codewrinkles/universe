using Codewrinkles.Application.Nova;
using Codewrinkles.API.Extensions;
using Kommand.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Codewrinkles.API.Modules.Nova;

public static class AlphaEndpoints
{
    public static void MapAlphaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/alpha")
            .WithTags("Alpha");

        // Public endpoint - no authentication required
        group.MapPost("apply", ApplyForAlpha)
            .WithName("ApplyForAlpha");

        // Requires authentication but NOT Nova access
        // (users redeem codes to GET Nova access)
        group.MapPost("redeem", RedeemAlphaCode)
            .WithName("RedeemAlphaCode")
            .RequireAuthorization();
    }

    private static async Task<IResult> ApplyForAlpha(
        [FromBody] ApplyForAlphaRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new ApplyForAlphaCommand(
            Email: request.Email,
            Name: request.Name,
            PrimaryTechStack: request.PrimaryTechStack,
            YearsOfExperience: request.YearsOfExperience,
            Goal: request.Goal);

        var result = await mediator.SendAsync(command, cancellationToken);

        if (result.AlreadyApplied)
        {
            return Results.Ok(new
            {
                success = true,
                message = "You've already applied! We'll be in touch soon.",
                alreadyApplied = true
            });
        }

        return Results.Created($"/api/alpha/applications/{result.ApplicationId}", new
        {
            success = true,
            message = "Thanks for applying! We'll review your application and get back to you within 48 hours.",
            alreadyApplied = false
        });
    }

    private static async Task<IResult> RedeemAlphaCode(
        HttpContext httpContext,
        [FromBody] RedeemAlphaCodeRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var profileId = httpContext.GetCurrentProfileId();

        var command = new RedeemAlphaCodeCommand(
            ProfileId: profileId,
            Code: request.Code);

        var result = await mediator.SendAsync(command, cancellationToken);

        return Results.Ok(new
        {
            success = true,
            message = "Welcome to Nova Alpha! You now have full access.",
            hasNovaAccess = result.HasNovaAccess,
            accessToken = result.AccessToken,
            refreshToken = result.RefreshToken
        });
    }
}

// Request DTOs
public sealed record ApplyForAlphaRequest(
    string Email,
    string Name,
    string PrimaryTechStack,
    int YearsOfExperience,
    string Goal
);

public sealed record RedeemAlphaCodeRequest(
    string Code
);
