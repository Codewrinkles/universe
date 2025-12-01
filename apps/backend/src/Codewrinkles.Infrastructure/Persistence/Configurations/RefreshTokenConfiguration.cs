using Codewrinkles.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", "identity");

        // Primary key with sequential GUID generation
        builder.HasKey(rt => rt.Id);

        // Explicitly configure sequential GUID generation
        // This avoids index fragmentation issues with random GUIDs
        builder.Property(rt => rt.Id)
            .ValueGeneratedOnAdd();

        // Token hash (indexed for fast lookups)
        builder.Property(rt => rt.TokenHash)
            .IsRequired()
            .HasMaxLength(128); // Base64 hash of 64-byte random value

        builder.HasIndex(rt => rt.TokenHash)
            .IsUnique(); // Ensure token uniqueness

        // Foreign key to Identity
        builder.Property(rt => rt.IdentityId)
            .IsRequired();

        builder.HasOne(rt => rt.Identity)
            .WithMany() // Identity doesn't need navigation to RefreshTokens
            .HasForeignKey(rt => rt.IdentityId)
            .OnDelete(DeleteBehavior.Cascade); // Delete tokens when identity is deleted

        // Expiration tracking
        builder.Property(rt => rt.CreatedAt)
            .IsRequired();

        builder.Property(rt => rt.ExpiresAt)
            .IsRequired();

        builder.HasIndex(rt => rt.ExpiresAt); // For cleanup queries

        // Usage tracking
        builder.Property(rt => rt.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(rt => rt.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(rt => rt.RevokedAt)
            .IsRequired(false);

        builder.Property(rt => rt.RevokedReason)
            .HasMaxLength(500)
            .IsRequired(false);

        // Token rotation tracking
        builder.Property(rt => rt.ReplacedByTokenId)
            .IsRequired(false);

        // Self-referencing relationship for token rotation
        builder.HasOne(rt => rt.ReplacedByToken)
            .WithMany()
            .HasForeignKey(rt => rt.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.NoAction); // Prevent cascade delete cycles

        // Composite index for fast queries by identity and validity
        builder.HasIndex(rt => new { rt.IdentityId, rt.IsUsed, rt.IsRevoked, rt.ExpiresAt });
    }
}
