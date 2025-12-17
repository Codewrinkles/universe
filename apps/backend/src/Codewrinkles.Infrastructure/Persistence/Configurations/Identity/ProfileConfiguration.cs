using Codewrinkles.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Identity;

public sealed class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        // Table configuration
        builder.ToTable("Profiles", "identity");

        // Primary key
        builder.HasKey(p => p.Id);

        // Primary key value generation - EF Core will use sequential GUID generation
        // This avoids index fragmentation issues with random GUIDs
        builder.Property(p => p.Id)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(p => p.IdentityId)
            .IsRequired();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Handle)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(p => p.Bio)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(p => p.AvatarUrl)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(p => p.Location)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(p => p.WebsiteUrl)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(p => p.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        // Nova access
        builder.Property(p => p.NovaAccess)
            .IsRequired()
            .HasDefaultValue(Domain.Nova.NovaAccessLevel.None);

        builder.Property(p => p.IsFoundingMember)
            .IsRequired()
            .HasDefaultValue(false);

        // Ignore computed properties
        builder.Ignore(p => p.HasNovaAccess);
        builder.Ignore(p => p.HasNovaProAccess);

        // Indexes
        builder.HasIndex(p => p.IdentityId)
            .IsUnique()
            .HasDatabaseName("IX_Profiles_IdentityId");

        builder.HasIndex(p => p.Handle)
            .IsUnique()
            .HasDatabaseName("IX_Profiles_Handle");
    }
}
