using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Pulse;

public sealed class HashtagConfiguration : IEntityTypeConfiguration<Domain.Pulse.Hashtag>
{
    public void Configure(EntityTypeBuilder<Domain.Pulse.Hashtag> builder)
    {
        // Table configuration
        builder.ToTable("Hashtags", "pulse");

        // Primary key
        builder.HasKey(h => h.Id);

        // Primary key value generation - EF Core will use sequential GUID generation
        // This avoids index fragmentation issues with random GUIDs
        builder.Property(h => h.Id)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(h => h.Tag)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(h => h.TagDisplay)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(h => h.PulseCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(h => h.LastUsedAt)
            .IsRequired();

        builder.Property(h => h.CreatedAt)
            .IsRequired();

        // Unique constraint on normalized tag (lowercase)
        builder.HasIndex(h => h.Tag)
            .IsUnique()
            .HasDatabaseName("UQ_Hashtags_Tag");

        // Composite index for trending hashtags query (ORDER BY PulseCount DESC, LastUsedAt DESC)
        builder.HasIndex(h => new { h.PulseCount, h.LastUsedAt })
            .HasDatabaseName("IX_Hashtags_PulseCount_LastUsedAt");
    }
}
