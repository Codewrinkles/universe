using Codewrinkles.Domain.Pulse;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Pulse;

public sealed class PulseLikeConfiguration : IEntityTypeConfiguration<PulseLike>
{
    public void Configure(EntityTypeBuilder<PulseLike> builder)
    {
        builder.ToTable("PulseLikes", "pulse");

        // Composite Primary Key
        builder.HasKey(pl => new { pl.PulseId, pl.ProfileId });

        // Properties
        builder.Property(pl => pl.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(pl => pl.Pulse)
            .WithMany()
            .HasForeignKey(pl => pl.PulseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pl => pl.Profile)
            .WithMany()
            .HasForeignKey(pl => pl.ProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(pl => pl.ProfileId)
            .HasDatabaseName("IX_PulseLikes_ProfileId");

        builder.HasIndex(pl => pl.CreatedAt)
            .HasDatabaseName("IX_PulseLikes_CreatedAt");
    }
}
