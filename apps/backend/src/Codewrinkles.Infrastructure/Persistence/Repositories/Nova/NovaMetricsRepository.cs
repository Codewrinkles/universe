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

        // Activated users: Nova users with 3+ sessions (including soft-deleted - they still represent real usage)
        var activatedUsers = 0;
        if (novaUsers > 0)
        {
            // Get profile IDs with Nova access
            var novaProfileIds = await _context.Profiles
                .Where(p => p.NovaAccess != NovaAccessLevel.None)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            // Count those with 3+ sessions (including soft-deleted)
            activatedUsers = await _context.ConversationSessions
                .Where(s => novaProfileIds.Contains(s.ProfileId))
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

        // Active in last 7 days: Nova users with session activity (including soft-deleted)
        var activeLast7Days = 0;
        if (novaUsers > 0)
        {
            var novaProfileIds = await _context.Profiles
                .Where(p => p.NovaAccess != NovaAccessLevel.None)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            activeLast7Days = await _context.ConversationSessions
                .Where(s => s.LastMessageAt >= sevenDaysAgo &&
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
        // Include soft-deleted sessions - they still represent real usage
        var totalSessions = await _context.ConversationSessions
            .CountAsync(cancellationToken);

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

    public async Task<IReadOnlyList<UserNovaUsageData>> GetUserUsageAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var last24Hours = now.AddHours(-24);
        var last3Days = now.AddDays(-3);
        var last7Days = now.AddDays(-7);
        var last14Days = now.AddDays(-14);
        var last30Days = now.AddDays(-30);

        // Get all Nova users with their profiles
        var novaProfiles = await _context.Profiles
            .AsNoTracking()
            .Where(p => p.NovaAccess != NovaAccessLevel.None)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Handle,
                p.AvatarUrl,
                p.NovaAccess
            })
            .ToListAsync(cancellationToken);

        if (novaProfiles.Count == 0)
        {
            return [];
        }

        var profileIds = novaProfiles.Select(p => p.Id).ToList();

        // Get session data for all Nova users in one query
        // Include soft-deleted sessions - they still represent real usage
        var sessionData = await _context.ConversationSessions
            .AsNoTracking()
            .Where(s => profileIds.Contains(s.ProfileId))
            .GroupBy(s => s.ProfileId)
            .Select(g => new
            {
                ProfileId = g.Key,
                Sessions24h = g.Count(s => s.CreatedAt >= last24Hours),
                Sessions3d = g.Count(s => s.CreatedAt >= last3Days),
                Sessions7d = g.Count(s => s.CreatedAt >= last7Days),
                Sessions30d = g.Count(s => s.CreatedAt >= last30Days),
                SessionsPrev7d = g.Count(s => s.CreatedAt >= last14Days && s.CreatedAt < last7Days),
                TotalSessions = g.Count(),
                LastActiveAt = g.Max(s => s.LastMessageAt),
                FirstSessionAt = g.Min(s => s.CreatedAt)
            })
            .ToListAsync(cancellationToken);

        // Get all session IDs for Nova users to count messages
        // Include soft-deleted sessions - they still represent real usage
        var novaSessionIds = await _context.ConversationSessions
            .AsNoTracking()
            .Where(s => profileIds.Contains(s.ProfileId))
            .Select(s => new { s.Id, s.ProfileId })
            .ToListAsync(cancellationToken);

        var sessionIdToProfileId = novaSessionIds.ToDictionary(s => s.Id, s => s.ProfileId);
        var sessionIds = novaSessionIds.Select(s => s.Id).ToList();

        // Get message counts per session
        var messageCounts = await _context.NovaMessages
            .AsNoTracking()
            .Where(m => sessionIds.Contains(m.SessionId))
            .GroupBy(m => m.SessionId)
            .Select(g => new
            {
                SessionId = g.Key,
                MessageCount = g.Count()
            })
            .ToListAsync(cancellationToken);

        // Aggregate message counts by profile
        var messageCountsByProfile = messageCounts
            .Where(m => sessionIdToProfileId.ContainsKey(m.SessionId))
            .GroupBy(m => sessionIdToProfileId[m.SessionId])
            .ToDictionary(
                g => g.Key,
                g => g.Sum(m => m.MessageCount)
            );

        // Combine data for each user
        var result = novaProfiles.Select(profile =>
        {
            var sessions = sessionData.FirstOrDefault(s => s.ProfileId == profile.Id);
            var totalMessages = messageCountsByProfile.GetValueOrDefault(profile.Id, 0);

            var totalSessions = sessions?.TotalSessions ?? 0;
            var avgMsgsPerSession = totalSessions > 0
                ? Math.Round((decimal)totalMessages / totalSessions, 1)
                : 0m;

            var sessions7d = sessions?.Sessions7d ?? 0;
            var sessionsPrev7d = sessions?.SessionsPrev7d ?? 0;
            var trendPercentage = sessionsPrev7d > 0
                ? Math.Round((decimal)(sessions7d - sessionsPrev7d) / sessionsPrev7d * 100, 1)
                : (sessions7d > 0 ? 100m : 0m);

            return new UserNovaUsageData(
                ProfileId: profile.Id,
                Name: profile.Name,
                Handle: profile.Handle,
                AvatarUrl: profile.AvatarUrl,
                AccessLevel: profile.NovaAccess,
                SessionsLast24Hours: sessions?.Sessions24h ?? 0,
                SessionsLast3Days: sessions?.Sessions3d ?? 0,
                SessionsLast7Days: sessions?.Sessions7d ?? 0,
                SessionsLast30Days: sessions?.Sessions30d ?? 0,
                TotalMessages: totalMessages,
                AvgMessagesPerSession: avgMsgsPerSession,
                LastActiveAt: sessions?.LastActiveAt,
                FirstSessionAt: sessions?.FirstSessionAt,
                SessionsPrevious7Days: sessionsPrev7d,
                TrendPercentage: trendPercentage
            );
        }).ToList();

        return result;
    }
}
