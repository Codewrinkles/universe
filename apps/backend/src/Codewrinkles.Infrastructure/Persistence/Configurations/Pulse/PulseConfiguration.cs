using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Pulse;

public sealed class PulseConfiguration : IEntityTypeConfiguration<Domain.Pulse.Pulse>
{
    public void Configure(EntityTypeBuilder<Domain.Pulse.Pulse> builder)
    {
        // Table configuration
        builder.ToTable("Pulses", "pulse");

        // Primary key
        builder.HasKey(p => p.Id);

        // Primary key value generation - EF Core will use sequential GUID generation
        // This avoids index fragmentation issues with random GUIDs
        builder.Property(p => p.Id)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(p => p.Content)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(p => p.Type)
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(p => p.UpdatedAt)
            .IsRequired(false);

        builder.Property(p => p.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Relationships
        builder.HasOne(p => p.Author)
            .WithMany()
            .HasForeignKey(p => p.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.RepulsedPulse)
            .WithMany()
            .HasForeignKey(p => p.RepulsedPulseId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(p => p.ParentPulse)
            .WithMany()
            .HasForeignKey(p => p.ParentPulseId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(p => p.Engagement)
            .WithOne(e => e.Pulse)
            .HasForeignKey<Domain.Pulse.PulseEngagement>(e => e.PulseId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        // Note: Image relationship is configured in PulseImageConfiguration
        // One-to-one relationships should only be configured on one side

        // Indexes
        // Composite index for GetPulsesByAuthor - covers (AuthorId, IsDeleted, CreatedAt DESC)
        // This is the most frequently used query pattern for profile pages
        builder.HasIndex(p => new { p.AuthorId, p.IsDeleted, p.CreatedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("IX_Pulses_AuthorId_IsDeleted_CreatedAt");

        // Composite index for feed queries - covers (IsDeleted, CreatedAt DESC)
        // Optimizes filtering out deleted pulses while ordering by recency
        builder.HasIndex(p => new { p.IsDeleted, p.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_Pulses_IsDeleted_CreatedAt");

        // Individual indexes kept for specific queries
        builder.HasIndex(p => p.AuthorId)
            .HasDatabaseName("IX_Pulses_AuthorId");

        builder.HasIndex(p => p.CreatedAt)
            .IsDescending()
            .HasDatabaseName("IX_Pulses_CreatedAt");

        builder.HasIndex(p => p.RepulsedPulseId)
            .HasDatabaseName("IX_Pulses_RepulsedPulseId")
            .HasFilter("[RepulsedPulseId] IS NOT NULL");

        builder.HasIndex(p => p.ParentPulseId)
            .HasDatabaseName("IX_Pulses_ParentPulseId")
            .HasFilter("[ParentPulseId] IS NOT NULL");

        builder.HasIndex(p => p.IsDeleted)
            .HasDatabaseName("IX_Pulses_IsDeleted");
    }
}
