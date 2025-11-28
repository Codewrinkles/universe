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

        builder.HasOne(p => p.Image)
            .WithOne(i => i.Pulse)
            .HasForeignKey<Domain.Pulse.PulseImage>(i => i.PulseId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        // Indexes
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
