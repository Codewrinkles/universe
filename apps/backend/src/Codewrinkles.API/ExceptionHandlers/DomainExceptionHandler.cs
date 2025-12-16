using Codewrinkles.Application.Common.Exceptions;
using Codewrinkles.Application.Users;
using Codewrinkles.Domain.Nova.Exceptions;
using Codewrinkles.Domain.Pulse.Exceptions;
using Codewrinkles.Domain.Social.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Codewrinkles.API.ExceptionHandlers;

/// <summary>
/// Global exception handler for domain and application layer exceptions.
/// Maps domain-specific exceptions to appropriate HTTP status codes and ProblemDetails responses.
/// This ensures consistent error responses across all endpoints without requiring explicit try-catch blocks.
/// </summary>
public sealed class DomainExceptionHandler : IExceptionHandler
{
    private readonly ILogger<DomainExceptionHandler> _logger;

    public DomainExceptionHandler(ILogger<DomainExceptionHandler> logger)
    {
        _logger = logger;
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

        // Log the exception with appropriate level
        LogException(exception, statusCode);

        var problemDetails = new ProblemDetails
        {
            Type = GetRfcTypeUrl(statusCode),
            Title = title,
            Status = statusCode,
            Detail = detail
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            // 400 Bad Request - Client errors / validation
            PulseContentEmptyException ex => (400, "Invalid Content", ex.Message),
            PulseContentTooLongException ex => (400, "Content Too Long", ex.Message),
            PulseAlreadyDeletedException ex => (400, "Already Deleted", ex.Message),
            PulseNotLikedException ex => (400, "Not Liked", ex.Message),
            FollowSelfException ex => (400, "Cannot Follow Self", ex.Message),
            NotFollowingException ex => (400, "Not Following", ex.Message),
            CurrentPasswordInvalidException ex => (400, "Invalid Password", ex.Message),
            InvalidImageException ex => (400, "Invalid Image", ex.Message),
            AuthorNotFoundException ex => (400, "Author Not Found", ex.Message),
            ProfileNotExistException ex => (400, "Profile Not Found", ex.Message),
            InvalidCursorException ex => (400, "Invalid Cursor", ex.Message),

            // 401 Unauthorized - Authentication failures
            InvalidCredentialsException => (401, "Invalid Credentials", "The email or password is incorrect."),
            InvalidRefreshTokenException ex => (401, "Invalid Refresh Token", ex.Message),
            RefreshTokenExpiredException ex => (401, "Refresh Token Expired", ex.Message),
            UnauthorizedAccessException ex => (401, "Unauthorized", ex.Message),

            // 403 Forbidden - Authorization failures
            AccountSuspendedException ex => (403, "Account Suspended", ex.Message),
            UnauthorizedPulseAccessException ex => (403, "Forbidden", ex.Message),
            UnauthorizedNotificationAccessException ex => (403, "Forbidden", ex.Message),

            // 403 Forbidden - Nova
            ConversationAccessDeniedException ex => (403, "Forbidden", ex.Message),

            // 404 Not Found
            ConversationNotFoundException ex => (404, "Conversation Not Found", ex.Message),
            PulseNotFoundException ex => (404, "Pulse Not Found", ex.Message),
            ProfileNotFoundException => (404, "Profile Not Found", "The requested profile was not found."),
            ProfileNotFoundByHandleException => (404, "Profile Not Found", "No profile exists with the specified handle."),
            CurrentUserProfileNotFoundException => (404, "Profile Not Found", "Current user's profile was not found."),
            IdentityNotFoundException => (404, "Identity Not Found", "The user identity was not found."),
            NotificationNotFoundException ex => (404, "Notification Not Found", ex.Message),

            // 409 Conflict
            PulseAlreadyLikedException ex => (409, "Already Liked", ex.Message),
            AlreadyFollowingException ex => (409, "Already Following", ex.Message),
            HandleAlreadyTakenException ex => (409, "Handle Already Taken", ex.Message),

            // 423 Locked
            AccountLockedException ex => (423, "Account Locked", ex.Message),

            // Not a domain exception we handle
            _ => (0, string.Empty, string.Empty)
        };
    }

    private void LogException(Exception exception, int statusCode)
    {
        // Log 4xx as warnings (client errors), 5xx would be errors (but we don't handle those here)
        if (statusCode >= 400 && statusCode < 500)
        {
            _logger.LogWarning(
                exception,
                "Domain exception occurred: {ExceptionType} - {Message}",
                exception.GetType().Name,
                exception.Message);
        }
    }

    private static string GetRfcTypeUrl(int statusCode) => statusCode switch
    {
        400 => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        401 => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
        403 => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
        404 => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        409 => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        423 => "https://tools.ietf.org/html/rfc4918#section-11.3",
        _ => "https://tools.ietf.org/html/rfc9110#section-15.6.1"
    };
}
