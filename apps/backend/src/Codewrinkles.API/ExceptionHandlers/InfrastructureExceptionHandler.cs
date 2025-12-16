using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Codewrinkles.API.ExceptionHandlers;

/// <summary>
/// Global exception handler for infrastructure-level exceptions.
/// Handles database failures, HTTP client errors, and other infrastructure concerns.
/// Returns appropriate HTTP status codes without exposing internal details.
/// </summary>
public sealed class InfrastructureExceptionHandler : IExceptionHandler
{
    private readonly ILogger<InfrastructureExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public InfrastructureExceptionHandler(
        ILogger<InfrastructureExceptionHandler> logger,
        IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = MapException(exception);

        // If we don't recognize the exception, let other handlers deal with it
        if (statusCode == 0)
        {
            return false;
        }

        // Always log infrastructure exceptions as errors
        _logger.LogError(
            exception,
            "Infrastructure exception occurred: {ExceptionType} - {Message}",
            exception.GetType().Name,
            exception.Message);

        var problemDetails = new ProblemDetails
        {
            Type = GetRfcTypeUrl(statusCode),
            Title = title,
            Status = statusCode,
            Detail = _environment.IsDevelopment() ? detail : GetSanitizedDetail(statusCode)
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            // Database exceptions - more specific type first (DbUpdateConcurrencyException derives from DbUpdateException)
            DbUpdateConcurrencyException ex => (409, "Concurrency Conflict", $"The resource was modified by another request: {ex.Message}"),
            DbUpdateException ex => (500, "Database Error", $"A database operation failed: {ex.InnerException?.Message ?? ex.Message}"),

            // HTTP client exceptions - return 502 Bad Gateway for external service failures
            HttpRequestException ex => (502, "External Service Error", $"An external service request failed: {ex.Message}"),

            // Timeout exceptions - return 504 Gateway Timeout
            TimeoutException ex => (504, "Request Timeout", $"The operation timed out: {ex.Message}"),

            // Not an infrastructure exception we handle
            _ => (0, string.Empty, string.Empty)
        };
    }

    private static string GetSanitizedDetail(int statusCode) => statusCode switch
    {
        502 => "An external service is temporarily unavailable. Please try again later.",
        503 => "The service is temporarily unavailable. Please try again later.",
        504 => "The request took too long to process. Please try again.",
        409 => "The resource was modified by another request. Please refresh and try again.",
        _ => "An unexpected error occurred. Please try again later."
    };

    private static string GetRfcTypeUrl(int statusCode) => statusCode switch
    {
        409 => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        502 => "https://tools.ietf.org/html/rfc9110#section-15.6.3",
        503 => "https://tools.ietf.org/html/rfc9110#section-15.6.4",
        504 => "https://tools.ietf.org/html/rfc9110#section-15.6.5",
        _ => "https://tools.ietf.org/html/rfc9110#section-15.6.1"
    };
}
