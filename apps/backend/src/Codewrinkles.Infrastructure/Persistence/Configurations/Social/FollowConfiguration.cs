using Codewrinkles.Domain.Social;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Social;

public sealed class FollowConfiguration : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        // Table mapping - will create [social].[Follows] table
        builder.ToTable("Follows", "social");

        // Composite Primary Key
        // This creates a clustered index on (FollowerId, FollowingId)
        builder.HasKey(f => new { f.FollowerId, f.FollowingId });

        // Properties
        builder.Property(f => f.CreatedAt)
            .IsRequired();

        // Relationships (cross-domain to Identity.Profile)
        builder.HasOne(f => f.Follower)
            .WithMany() // Profile doesn't need reverse navigation (one-way)
            .HasForeignKey(f => f.FollowerId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade deletes

        builder.HasOne(f => f.Following)
            .WithMany()
            .HasForeignKey(f => f.FollowingId)
            .OnDelete(DeleteBehavior.Restrict);

        // CRITICAL INDEXES FOR PERFORMANCE

        // Index 1: Get all people I'm following (for feed query)
        // Covering index includes CreatedAt to avoid key lookups
        builder.HasIndex(f => f.FollowerId)
            .HasDatabaseName("IX_Follows_FollowerId_FollowingId")
            .IncludeProperties(f => new { f.FollowingId, f.CreatedAt });

        // Index 2: Get all my followers (for follower list + suggestions)
        // Covering index includes CreatedAt to avoid key lookups
        builder.HasIndex(f => f.FollowingId)
            .HasDatabaseName("IX_Follows_FollowingId_FollowerId")
            .IncludeProperties(f => new { f.FollowerId, f.CreatedAt });

        // Note: No need for index on (FollowerId, FollowingId)
        // because composite PK already creates this index
    }
}
