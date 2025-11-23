using Codewrinkles.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Identity;

public sealed class IdentityConfiguration : IEntityTypeConfiguration<Domain.Identity.Identity>
{
    public void Configure(EntityTypeBuilder<Domain.Identity.Identity> builder)
    {
        // Table configuration
        builder.ToTable("Identities", "identity");

        // Primary key
        builder.HasKey(i => i.Id);

        // Primary key value generation - EF Core will use sequential GUID generation
        // This avoids index fragmentation issues with random GUIDs
        builder.Property(i => i.Id)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(i => i.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(i => i.EmailNormalized)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(i => i.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(i => i.IsEmailVerified)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(i => i.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(i => i.FailedLoginAttempts)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(i => i.LockedUntil)
            .IsRequired(false);

        builder.Property(i => i.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(i => i.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(i => i.LastLoginAt)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(i => i.EmailNormalized)
            .IsUnique()
            .HasDatabaseName("IX_Identities_EmailNormalized");

        builder.HasIndex(i => i.IsActive)
            .HasDatabaseName("IX_Identities_IsActive");

        builder.HasIndex(i => i.CreatedAt)
            .HasDatabaseName("IX_Identities_CreatedAt");

        // Relationships
        builder.HasOne(i => i.Profile)
            .WithOne(p => p.Identity)
            .HasForeignKey<Profile>(p => p.IdentityId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
