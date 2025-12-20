using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Identity;
using Codewrinkles.Domain.Nova;
using Microsoft.EntityFrameworkCore;

namespace Codewrinkles.Infrastructure.Persistence.Repositories.Nova;

public sealed class NovaMetricsRepository : INovaMetricsRepository
{
    private readonly ApplicationDbContext _context;

    public NovaMetricsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NovaAlphaMetricsData> GetAlphaMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        // ============================================
        // APPLICATION FUNNEL
        // ============================================
        var totalApplications = await _context.AlphaApplications
            .CountAsync(cancellationToken);

        var pendingApplications = await _context.AlphaApplications
            .CountAsync(a => a.Status == AlphaApplicationStatus.Pending, cancellationToken);

        var acceptedApplications = await _context.AlphaApplications
            .CountAsync(a => a.Status == AlphaApplicationStatus.Accepted, cancellationToken);

        var waitlistedApplications = await _context.AlphaApplications
            .CountAsync(a => a.Status == AlphaApplicationStatus.Waitlisted, cancellationToken);

        var redeemedCodes = await _context.AlphaApplications
            .CountAsync(a => a.InviteCodeRedeemed, cancellationToken);

        // ============================================
        // USER METRICS
        // ============================================

        // Nova users: profiles with NovaAccess != None
        var novaUsers = await _context.Profiles
            .CountAsync(p => p.NovaAccess != NovaAccessLevel.None, cancellationToken);

        // Activated users: Nova users with 3+ non-deleted sessions
        var activatedUsers = 0;
        if (novaUsers > 0)
        {
            // Get profile IDs with Nova access
            var novaProfileIds = await _context.Profiles
                .Where(p => p.NovaAccess != NovaAccessLevel.None)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            // Count those with 3+ sessions
            activatedUsers = await _context.ConversationSessions
                .Where(s => !s.IsDeleted && novaProfileIds.Contains(s.ProfileId))
                .GroupBy(s => s.ProfileId)
                .CountAsync(g => g.Count() >= 3, cancellationToken);
        }

        // Activation rate
        var activationRate = novaUsers > 0
            ? Math.Round((decimal)activatedUsers / novaUsers * 100, 1)
            : 0m;

        // ============================================
        // ENGAGEMENT METRICS
        // ============================================
        var sevenDaysAgo = DateTimeOffset.UtcNow.AddDays(-7);

        // Active in last 7 days: Nova users with session activity
        var activeLast7Days = 0;
        if (novaUsers > 0)
        {
            var novaProfileIds = await _context.Profiles
                .Where(p => p.NovaAccess != NovaAccessLevel.None)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            activeLast7Days = await _context.ConversationSessions
                .Where(s => !s.IsDeleted &&
                           s.LastMessageAt >= sevenDaysAgo &&
                           novaProfileIds.Contains(s.ProfileId))
                .Select(s => s.ProfileId)
                .Distinct()
                .CountAsync(cancellationToken);
        }

        // Active rate
        var activeRate = novaUsers > 0
            ? Math.Round((decimal)activeLast7Days / novaUsers * 100, 1)
            : 0m;

        // ============================================
        // USAGE METRICS
        // ============================================
        var totalSessions = await _context.ConversationSessions
            .CountAsync(s => !s.IsDeleted, cancellationToken);

        var totalMessages = await _context.NovaMessages.CountAsync(cancellationToken);

        var avgSessionsPerUser = novaUsers > 0
            ? Math.Round((decimal)totalSessions / novaUsers, 1)
            : 0m;

        var avgMessagesPerSession = totalSessions > 0
            ? Math.Round((decimal)totalMessages / totalSessions, 1)
            : 0m;

        // ============================================
        // RETURN RESULT
        // ============================================
        return new NovaAlphaMetricsData(
            TotalApplications: totalApplications,
            PendingApplications: pendingApplications,
            AcceptedApplications: acceptedApplications,
            WaitlistedApplications: waitlistedApplications,
            RedeemedCodes: redeemedCodes,
            NovaUsers: novaUsers,
            ActivatedUsers: activatedUsers,
            ActivationRate: activationRate,
            ActiveLast7Days: activeLast7Days,
            ActiveRate: activeRate,
            TotalSessions: totalSessions,
            TotalMessages: totalMessages,
            AvgSessionsPerUser: avgSessionsPerUser,
            AvgMessagesPerSession: avgMessagesPerSession
        );
    }
}
