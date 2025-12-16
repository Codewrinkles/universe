using Codewrinkles.Domain.Identity;
using Codewrinkles.Domain.Notification;
using Codewrinkles.Domain.Nova;
using Codewrinkles.Domain.Pulse;
using Codewrinkles.Domain.Social;
using Microsoft.EntityFrameworkCore;

namespace Codewrinkles.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Identity schema
    public DbSet<Identity> Identities => Set<Identity>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Pulse schema
    public DbSet<Pulse> Pulses => Set<Pulse>();
    public DbSet<PulseEngagement> PulseEngagements => Set<PulseEngagement>();
    public DbSet<PulseLike> PulseLikes => Set<PulseLike>();
    public DbSet<PulseImage> PulseImages => Set<PulseImage>();
    public DbSet<PulseLinkPreview> PulseLinkPreviews => Set<PulseLinkPreview>();
    public DbSet<PulseMention> PulseMentions => Set<PulseMention>();
    public DbSet<PulseBookmark> PulseBookmarks => Set<PulseBookmark>();
    public DbSet<Hashtag> Hashtags => Set<Hashtag>();
    public DbSet<PulseHashtag> PulseHashtags => Set<PulseHashtag>();

    // Social schema
    public DbSet<Follow> Follows => Set<Follow>();

    // Notification schema
    public DbSet<Notification> Notifications => Set<Notification>();

    // Nova schema
    public DbSet<ConversationSession> ConversationSessions => Set<ConversationSession>();
    public DbSet<Message> NovaMessages => Set<Message>();
    public DbSet<LearnerProfile> LearnerProfiles => Set<LearnerProfile>();
    public DbSet<Memory> Memories => Set<Memory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
