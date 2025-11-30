using Codewrinkles.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Identity;

public sealed class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.ToTable("ExternalLogins", "identity");
        builder.HasKey(el => el.Id);

        // Explicitly configure sequential GUID generation
        // This avoids index fragmentation issues with random GUIDs
        builder.Property(el => el.Id)
            .ValueGeneratedOnAdd();

        builder.Property(el => el.Provider)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(el => el.ProviderUserId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(el => el.ProviderEmail)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(el => el.ProviderDisplayName)
            .HasMaxLength(255);

        builder.Property(el => el.ProviderAvatarUrl)
            .HasMaxLength(500);

        builder.Property(el => el.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(el => el.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Unique constraint on Provider + ProviderUserId
        builder.HasIndex(el => new { el.Provider, el.ProviderUserId })
            .IsUnique()
            .HasDatabaseName("UQ_ExternalLogins_Provider_UserId");

        // Index on IdentityId for faster lookups
        builder.HasIndex(el => el.IdentityId)
            .HasDatabaseName("IX_ExternalLogins_IdentityId");

        // Relationship with Identity
        builder.HasOne(el => el.Identity)
            .WithMany(i => i.ExternalLogins)
            .HasForeignKey(el => el.IdentityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
