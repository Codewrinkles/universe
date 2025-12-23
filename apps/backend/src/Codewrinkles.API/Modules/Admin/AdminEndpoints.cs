using Codewrinkles.Application.Admin;
using Codewrinkles.Application.Nova;
using Codewrinkles.Domain.Nova;
using Kommand.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Codewrinkles.API.Modules.Admin;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/dashboard/metrics", GetDashboardMetrics)
            .WithName("GetDashboardMetrics");

        // Alpha application management
        group.MapGet("/alpha/applications", GetAlphaApplications)
            .WithName("GetAlphaApplications");

        group.MapPost("/alpha/applications/{id:guid}/accept", AcceptAlphaApplication)
            .WithName("AcceptAlphaApplication");

        group.MapPost("/alpha/applications/{id:guid}/waitlist", WaitlistAlphaApplication)
            .WithName("WaitlistAlphaApplication");

        // Nova metrics
        group.MapGet("/nova/metrics", GetNovaAlphaMetrics)
            .WithName("GetNovaAlphaMetrics");

        group.MapGet("/nova/metrics/users", GetNovaUserUsage)
            .WithName("GetNovaUserUsage");

        // User management
        group.MapGet("/users", GetAdminUsers)
            .WithName("GetAdminUsers");

        // Content ingestion management
        group.MapGet("/nova/content/jobs", GetIngestionJobs)
            .WithName("GetIngestionJobs");

        group.MapGet("/nova/content/jobs/{id:guid}", GetIngestionJob)
            .WithName("GetIngestionJob");

        group.MapPost("/nova/content/pdf", IngestPdf)
            .WithName("IngestPdf")
            .DisableAntiforgery();

        group.MapPost("/nova/content/transcript", IngestTranscript)
            .WithName("IngestTranscript");

        group.MapPost("/nova/content/docs", IngestDocs)
            .WithName("IngestDocs");

        group.MapDelete("/nova/content/jobs/{id:guid}", DeleteIngestionJob)
            .WithName("DeleteIngestionJob");
    }

    private static async Task<IResult> GetDashboardMetrics(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetDashboardMetricsQuery();
        var result = await mediator.SendAsync(query, cancellationToken);

        return Results.Ok(new
        {
            totalUsers = result.TotalUsers,
            activeUsers = result.ActiveUsers,
            totalPulses = result.TotalPulses
        });
    }

    private static async Task<IResult> GetAlphaApplications(
        [FromServices] IMediator mediator,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        AlphaApplicationStatus? statusFilter = status?.ToLowerInvariant() switch
        {
            "pending" => AlphaApplicationStatus.Pending,
            "accepted" => AlphaApplicationStatus.Accepted,
            "waitlisted" => AlphaApplicationStatus.Waitlisted,
            _ => null
        };

        var query = new GetAlphaApplicationsQuery(statusFilter);
        var result = await mediator.SendAsync(query, cancellationToken);

        return Results.Ok(new
        {
            applications = result.Applications.Select(a => new
            {
                id = a.Id,
                email = a.Email,
                name = a.Name,
                primaryTechStack = a.PrimaryTechStack,
                yearsOfExperience = a.YearsOfExperience,
                goal = a.Goal,
                status = a.Status.ToString().ToLowerInvariant(),
                inviteCode = a.InviteCode,
                inviteCodeRedeemed = a.InviteCodeRedeemed,
                createdAt = a.CreatedAt
            })
        });
    }

    private static async Task<IResult> AcceptAlphaApplication(
        [FromServices] IMediator mediator,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new AcceptAlphaApplicationCommand(id);
            var result = await mediator.SendAsync(command, cancellationToken);

            return Results.Ok(new
            {
                inviteCode = result.InviteCode,
                message = "Application accepted. Invite code email sent."
            });
        }
        catch (AlphaApplicationNotFoundException)
        {
            return Results.NotFound(new { message = "Application not found" });
        }
        catch (AlphaApplicationNotPendingException)
        {
            return Results.BadRequest(new { message = "Application is not in pending status" });
        }
    }

    private static async Task<IResult> WaitlistAlphaApplication(
        [FromServices] IMediator mediator,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new WaitlistAlphaApplicationCommand(id);
            await mediator.SendAsync(command, cancellationToken);

            return Results.Ok(new
            {
                message = "Application waitlisted. Notification email sent."
            });
        }
        catch (AlphaApplicationNotFoundException)
        {
            return Results.NotFound(new { message = "Application not found" });
        }
        catch (AlphaApplicationNotPendingException)
        {
            return Results.BadRequest(new { message = "Application is not in pending status" });
        }
    }

    private static async Task<IResult> GetNovaAlphaMetrics(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetNovaAlphaMetricsQuery();
        var result = await mediator.QueryAsync(query, cancellationToken);

        return Results.Ok(new
        {
            // Application funnel
            totalApplications = result.TotalApplications,
            pendingApplications = result.PendingApplications,
            acceptedApplications = result.AcceptedApplications,
            waitlistedApplications = result.WaitlistedApplications,
            redeemedCodes = result.RedeemedCodes,

            // User metrics
            novaUsers = result.NovaUsers,
            activatedUsers = result.ActivatedUsers,
            activationRate = result.ActivationRate,

            // Engagement metrics
            activeLast7Days = result.ActiveLast7Days,
            activeRate = result.ActiveRate,

            // Usage metrics
            totalSessions = result.TotalSessions,
            totalMessages = result.TotalMessages,
            avgSessionsPerUser = result.AvgSessionsPerUser,
            avgMessagesPerSession = result.AvgMessagesPerSession
        });
    }

    private static async Task<IResult> GetAdminUsers(
        [FromServices] IMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Clamp page size to reasonable limits
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var query = new GetAdminUsersQuery(page, pageSize);
        var result = await mediator.SendAsync(query, cancellationToken);

        return Results.Ok(new
        {
            users = result.Users.Select(u => new
            {
                profileId = u.ProfileId,
                name = u.Name,
                handle = u.Handle,
                avatarUrl = u.AvatarUrl,
                email = u.Email,
                createdAt = u.CreatedAt
            }),
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
            totalPages = result.TotalPages
        });
    }

    private static async Task<IResult> GetNovaUserUsage(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetNovaUserUsageQuery();
        var result = await mediator.QueryAsync(query, cancellationToken);

        return Results.Ok(new
        {
            users = result.Users.Select(u => new
            {
                profileId = u.ProfileId,
                name = u.Name,
                handle = u.Handle,
                avatarUrl = u.AvatarUrl,
                accessLevel = (int)u.AccessLevel,
                accessLevelName = u.AccessLevel.ToString(),
                sessionsLast24Hours = u.SessionsLast24Hours,
                sessionsLast3Days = u.SessionsLast3Days,
                sessionsLast7Days = u.SessionsLast7Days,
                sessionsLast30Days = u.SessionsLast30Days,
                totalMessages = u.TotalMessages,
                avgMessagesPerSession = u.AvgMessagesPerSession,
                lastActiveAt = u.LastActiveAt,
                firstSessionAt = u.FirstSessionAt,
                sessionsPrevious7Days = u.SessionsPrevious7Days,
                trendPercentage = u.TrendPercentage
            })
        });
    }

    private static async Task<IResult> GetIngestionJobs(
        [FromServices] IMediator mediator,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        IngestionJobStatus? statusFilter = status?.ToLowerInvariant() switch
        {
            "queued" => IngestionJobStatus.Queued,
            "processing" => IngestionJobStatus.Processing,
            "completed" => IngestionJobStatus.Completed,
            "failed" => IngestionJobStatus.Failed,
            _ => null
        };

        var query = new GetIngestionJobsQuery(statusFilter);
        var result = await mediator.QueryAsync(query, cancellationToken);

        return Results.Ok(new
        {
            jobs = result.Jobs.Select(j => new
            {
                id = j.Id,
                source = j.Source.ToLowerInvariant(),
                status = j.Status.ToLowerInvariant(),
                title = j.Title,
                author = j.Author,
                technology = j.Technology,
                sourceUrl = j.SourceUrl,
                chunksCreated = j.ChunksCreated,
                totalPages = j.TotalPages,
                pagesProcessed = j.PagesProcessed,
                errorMessage = j.ErrorMessage,
                createdAt = j.CreatedAt,
                startedAt = j.StartedAt,
                completedAt = j.CompletedAt
            })
        });
    }

    private static async Task<IResult> GetIngestionJob(
        [FromServices] IMediator mediator,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetIngestionJobQuery(id);
        var result = await mediator.QueryAsync(query, cancellationToken);

        if (result.Job is null)
        {
            return Results.NotFound(new { message = "Ingestion job not found" });
        }

        var j = result.Job;
        return Results.Ok(new
        {
            id = j.Id,
            source = j.Source.ToLowerInvariant(),
            status = j.Status.ToLowerInvariant(),
            title = j.Title,
            author = j.Author,
            technology = j.Technology,
            sourceUrl = j.SourceUrl,
            chunksCreated = j.ChunksCreated,
            totalPages = j.TotalPages,
            pagesProcessed = j.PagesProcessed,
            errorMessage = j.ErrorMessage,
            createdAt = j.CreatedAt,
            startedAt = j.StartedAt,
            completedAt = j.CompletedAt
        });
    }

    private static async Task<IResult> IngestPdf(
        [FromServices] IMediator mediator,
        [FromForm] string title,
        [FromForm] string? contentType,
        [FromForm] string? author,
        [FromForm] string? technology,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new { message = "PDF file is required" });
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return Results.BadRequest(new { message = "Title is required" });
        }

        // Default to "book" for backwards compatibility
        var normalizedContentType = string.IsNullOrWhiteSpace(contentType)
            ? "book"
            : contentType.Trim().ToLowerInvariant();
        if (normalizedContentType == "book" && string.IsNullOrWhiteSpace(author))
        {
            return Results.BadRequest(new { message = "Author is required for books" });
        }

        if (normalizedContentType == "officialdocs" && string.IsNullOrWhiteSpace(technology))
        {
            return Results.BadRequest(new { message = "Technology is required for official documentation" });
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);

        var command = new IngestPdfCommand(
            Title: title.Trim(),
            PdfBytes: memoryStream.ToArray(),
            ContentType: normalizedContentType,
            Author: author?.Trim(),
            Technology: technology?.Trim());

        var result = await mediator.SendAsync(command, cancellationToken);

        return Results.Accepted(
            $"/api/admin/nova/content/jobs/{result.JobId}",
            new
            {
                jobId = result.JobId,
                message = "PDF ingestion started. Check job status for progress."
            });
    }

    private static async Task<IResult> IngestTranscript(
        [FromServices] IMediator mediator,
        [FromBody] IngestTranscriptRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.VideoUrl))
        {
            return Results.BadRequest(new { message = "Video URL is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Results.BadRequest(new { message = "Title is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Transcript))
        {
            return Results.BadRequest(new { message = "Transcript is required" });
        }

        // Extract video ID from URL
        var videoId = ExtractYouTubeVideoId(request.VideoUrl);
        if (string.IsNullOrEmpty(videoId))
        {
            return Results.BadRequest(new { message = "Invalid YouTube URL" });
        }

        var command = new IngestTranscriptCommand(
            VideoId: videoId,
            VideoUrl: request.VideoUrl.Trim(),
            Title: request.Title.Trim(),
            Transcript: request.Transcript);

        var result = await mediator.SendAsync(command, cancellationToken);

        return Results.Accepted(
            $"/api/admin/nova/content/jobs/{result.JobId}",
            new
            {
                jobId = result.JobId,
                message = "Transcript ingestion started. Check job status for progress."
            });
    }

    private static async Task<IResult> IngestDocs(
        [FromServices] IMediator mediator,
        [FromBody] IngestDocsRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.HomepageUrl))
        {
            return Results.BadRequest(new { message = "Homepage URL is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Technology))
        {
            return Results.BadRequest(new { message = "Technology is required" });
        }

        // Validate URL
        if (!Uri.TryCreate(request.HomepageUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            return Results.BadRequest(new { message = "Invalid URL format" });
        }

        var maxPages = request.MaxPages ?? 100;
        if (maxPages < 1 || maxPages > 500)
        {
            return Results.BadRequest(new { message = "MaxPages must be between 1 and 500" });
        }

        var command = new IngestDocsCommand(
            HomepageUrl: request.HomepageUrl.Trim(),
            Technology: request.Technology.Trim(),
            MaxPages: maxPages);

        var result = await mediator.SendAsync(command, cancellationToken);

        return Results.Accepted(
            $"/api/admin/nova/content/jobs/{result.JobId}",
            new
            {
                jobId = result.JobId,
                message = "Documentation scraping started. Check job status for progress."
            });
    }

    private static async Task<IResult> DeleteIngestionJob(
        [FromServices] IMediator mediator,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteIngestionJobCommand(id);
        var result = await mediator.SendAsync(command, cancellationToken);

        if (!result.Success)
        {
            if (result.Message == "Ingestion job not found")
            {
                return Results.NotFound(new { message = result.Message });
            }
            return Results.BadRequest(new { message = result.Message });
        }

        return Results.Ok(new { message = "Ingestion job and associated content deleted" });
    }

    private static string? ExtractYouTubeVideoId(string url)
    {
        // Handle various YouTube URL formats
        // - https://www.youtube.com/watch?v=VIDEO_ID
        // - https://youtu.be/VIDEO_ID
        // - https://www.youtube.com/embed/VIDEO_ID

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return null;
        }

        if (uri.Host.Contains("youtube.com"))
        {
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            return query["v"];
        }

        if (uri.Host.Contains("youtu.be"))
        {
            return uri.AbsolutePath.TrimStart('/');
        }

        return null;
    }
}

public sealed record IngestTranscriptRequest(
    string VideoUrl,
    string Title,
    string Transcript
);

public sealed record IngestDocsRequest(
    string HomepageUrl,
    string Technology,
    int? MaxPages
);
