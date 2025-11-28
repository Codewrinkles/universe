using Codewrinkles.Domain.Pulse;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Pulse;

public sealed class PulseMentionConfiguration : IEntityTypeConfiguration<PulseMention>
{
    public void Configure(EntityTypeBuilder<PulseMention> builder)
    {
        builder.ToTable("PulseMentions", "pulse");

        // Composite primary key
        builder.HasKey(m => new { m.PulseId, m.ProfileId });

        // Properties
        builder.Property(m => m.Handle)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(m => m.Pulse)
            .WithMany()
            .HasForeignKey(m => m.PulseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.MentionedProfile)
            .WithMany()
            .HasForeignKey(m => m.ProfileId)
            .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete when profile is deleted

        // Indexes
        builder.HasIndex(m => m.Handle);
        builder.HasIndex(m => m.CreatedAt);
    }
}
