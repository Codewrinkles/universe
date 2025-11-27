using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Pulse;

public sealed class PulseEngagementConfiguration : IEntityTypeConfiguration<Domain.Pulse.PulseEngagement>
{
    public void Configure(EntityTypeBuilder<Domain.Pulse.PulseEngagement> builder)
    {
        // Table configuration
        builder.ToTable("PulseEngagements", "pulse");

        // Primary key
        builder.HasKey(e => e.PulseId);

        // Properties
        builder.Property(e => e.ReplyCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.RepulseCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.LikeCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.ViewCount)
            .IsRequired()
            .HasDefaultValue(0L);

        // Relationship (inverse side configured in PulseConfiguration)
        builder.HasOne(e => e.Pulse)
            .WithOne(p => p.Engagement)
            .HasForeignKey<Domain.Pulse.PulseEngagement>(e => e.PulseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
