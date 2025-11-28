using Codewrinkles.Domain.Identity;
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

    // Pulse schema
    public DbSet<Pulse> Pulses => Set<Pulse>();
    public DbSet<PulseEngagement> PulseEngagements => Set<PulseEngagement>();
    public DbSet<PulseLike> PulseLikes => Set<PulseLike>();
    public DbSet<PulseImage> PulseImages => Set<PulseImage>();
    public DbSet<PulseMention> PulseMentions => Set<PulseMention>();

    // Social schema
    public DbSet<Follow> Follows => Set<Follow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
