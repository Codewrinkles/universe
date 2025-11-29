using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Pulse;

public sealed class PulseHashtagConfiguration : IEntityTypeConfiguration<Domain.Pulse.PulseHashtag>
{
    public void Configure(EntityTypeBuilder<Domain.Pulse.PulseHashtag> builder)
    {
        // Table configuration
        builder.ToTable("PulseHashtags", "pulse");

        // Composite primary key (PulseId, HashtagId)
        builder.HasKey(ph => new { ph.PulseId, ph.HashtagId });

        // Properties
        builder.Property(ph => ph.Position)
            .IsRequired();

        builder.Property(ph => ph.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(ph => ph.Pulse)
            .WithMany()
            .HasForeignKey(ph => ph.PulseId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne(ph => ph.Hashtag)
            .WithMany(h => h.PulseHashtags)
            .HasForeignKey(ph => ph.HashtagId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        // Index for querying pulses by hashtag
        builder.HasIndex(ph => ph.HashtagId)
            .HasDatabaseName("IX_PulseHashtags_HashtagId");
    }
}
