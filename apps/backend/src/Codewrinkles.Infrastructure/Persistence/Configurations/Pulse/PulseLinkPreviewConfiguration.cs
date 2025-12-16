using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Pulse;

public sealed class PulseLinkPreviewConfiguration : IEntityTypeConfiguration<Domain.Pulse.PulseLinkPreview>
{
    public void Configure(EntityTypeBuilder<Domain.Pulse.PulseLinkPreview> builder)
    {
        // Table configuration
        builder.ToTable("PulseLinkPreviews", "pulse");

        // Primary key
        builder.HasKey(p => p.Id);

        // Primary key value generation - EF Core will use sequential GUID generation
        // This avoids index fragmentation issues with random GUIDs
        builder.Property(p => p.Id)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(p => p.Url)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(p => p.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(p => p.ImageUrl)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(p => p.Domain)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        // Foreign key - PulseId must be unique for one-to-one relationship
        builder.Property(p => p.PulseId)
            .IsRequired();

        builder.HasIndex(p => p.PulseId)
            .IsUnique()
            .HasDatabaseName("IX_PulseLinkPreviews_PulseId");

        // Relationship - one-to-one with Pulse
        // Configure on the dependent side (PulseLinkPreview has the FK)
        builder.HasOne(p => p.Pulse)
            .WithOne(p => p.LinkPreview)
            .HasForeignKey<Domain.Pulse.PulseLinkPreview>(p => p.PulseId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);
    }
}
