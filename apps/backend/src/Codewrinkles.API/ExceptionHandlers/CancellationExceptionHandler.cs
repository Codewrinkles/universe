using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Codewrinkles.API.ExceptionHandlers;

/// <summary>
/// Global exception handler for operation cancellation exceptions.
/// Handles cases where requests are cancelled (client disconnect) or operations timeout.
/// Returns appropriate HTTP status codes for cancelled operations.
/// </summary>
public sealed class CancellationExceptionHandler : IExceptionHandler
{
    private readonly ILogger<CancellationExceptionHandler> _logger;

    public CancellationExceptionHandler(ILogger<CancellationExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Handle OperationCanceledException and its derivatives (TaskCanceledException)
        if (exception is not OperationCanceledException canceledException)
        {
            return false;
        }

        // Determine if this was a client disconnect vs a server-side timeout
        var isClientDisconnect = httpContext.RequestAborted.IsCancellationRequested;

        if (isClientDisconnect)
        {
            // Client disconnected - log at debug level (not an error)
            _logger.LogDebug(
                "Request cancelled by client: {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);

            // 499 Client Closed Request (nginx convention, widely understood)
            httpContext.Response.StatusCode = 499;
            return true;
        }

        // Server-side timeout - log as warning
        _logger.LogWarning(
            canceledException,
            "Operation timed out: {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.9",
            Title = "Request Timeout",
            Status = StatusCodes.Status408RequestTimeout,
            Detail = "The operation took too long to complete and was cancelled."
        };

        httpContext.Response.StatusCode = StatusCodes.Status408RequestTimeout;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
