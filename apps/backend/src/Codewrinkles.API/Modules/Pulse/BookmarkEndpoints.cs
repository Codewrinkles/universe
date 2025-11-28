using Codewrinkles.API.Extensions;
using Codewrinkles.Application.Pulse;
using Kommand.Abstractions;

namespace Codewrinkles.API.Modules.Pulse;

public static class BookmarkEndpoints
{
    public static void MapBookmarkEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/pulse/{pulseId:guid}/bookmark")
            .WithTags("Pulse Bookmarks");

        // POST /api/pulse/{pulseId}/bookmark - Bookmark a pulse
        group.MapPost("", async (
            Guid pulseId,
            IMediator mediator,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var profileId = context.GetCurrentProfileId();
            var command = new BookmarkPulseCommand(profileId, pulseId);
            await mediator.SendAsync(command, cancellationToken);
            return Results.NoContent();
        });

        // DELETE /api/pulse/{pulseId}/bookmark - Remove bookmark
        group.MapDelete("", async (
            Guid pulseId,
            IMediator mediator,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var profileId = context.GetCurrentProfileId();
            var command = new UnbookmarkPulseCommand(profileId, pulseId);
            await mediator.SendAsync(command, cancellationToken);
            return Results.NoContent();
        });

        // GET /api/bookmarks - Get bookmarked pulses
        app.MapGet("/api/bookmarks", async (
            string? cursor,
            int? limit,
            IMediator mediator,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var profileId = context.GetCurrentProfileId();
            var effectiveLimit = limit ?? 20;
            var query = new GetBookmarkedPulsesQuery(profileId, cursor, effectiveLimit);
            var result = await mediator.SendAsync(query, cancellationToken);
            return Results.Ok(result);
        })
        .WithTags("Pulse Bookmarks");
    }
}
