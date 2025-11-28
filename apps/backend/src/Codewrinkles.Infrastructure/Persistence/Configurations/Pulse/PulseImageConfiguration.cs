using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Pulse;

public sealed class PulseImageConfiguration : IEntityTypeConfiguration<Domain.Pulse.PulseImage>
{
    public void Configure(EntityTypeBuilder<Domain.Pulse.PulseImage> builder)
    {
        // Table configuration
        builder.ToTable("PulseImages", "pulse");

        // Primary key
        builder.HasKey(i => i.Id);

        // Primary key value generation - EF Core will use sequential GUID generation
        // This avoids index fragmentation issues with random GUIDs
        builder.Property(i => i.Id)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(i => i.Url)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(i => i.AltText)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(i => i.Width)
            .IsRequired();

        builder.Property(i => i.Height)
            .IsRequired();

        // Foreign key - PulseId must be unique for one-to-one relationship
        builder.Property(i => i.PulseId)
            .IsRequired();

        builder.HasIndex(i => i.PulseId)
            .IsUnique()
            .HasDatabaseName("IX_PulseImages_PulseId");

        // Relationship - one-to-one with Pulse
        builder.HasOne(i => i.Pulse)
            .WithOne()
            .HasForeignKey<Domain.Pulse.PulseImage>(i => i.PulseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
