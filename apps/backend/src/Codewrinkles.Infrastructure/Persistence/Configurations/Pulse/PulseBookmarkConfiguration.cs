using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Codewrinkles.Domain.Pulse;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Pulse;

public sealed class PulseBookmarkConfiguration : IEntityTypeConfiguration<PulseBookmark>
{
    public void Configure(EntityTypeBuilder<PulseBookmark> builder)
    {
        builder.ToTable("PulseBookmarks", "pulse");

        builder.HasKey(b => b.Id);

        // Explicitly configure sequential GUID generation
        // This avoids index fragmentation issues with random GUIDs
        builder.Property(b => b.Id)
            .ValueGeneratedOnAdd();

        builder.Property(b => b.PulseId)
            .IsRequired();

        builder.Property(b => b.ProfileId)
            .IsRequired();

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(b => b.Pulse)
            .WithMany()
            .HasForeignKey(b => b.PulseId)
            .OnDelete(DeleteBehavior.Cascade); // If pulse is deleted, delete bookmarks

        builder.HasOne(b => b.Profile)
            .WithMany()
            .HasForeignKey(b => b.ProfileId)
            .OnDelete(DeleteBehavior.Cascade); // If profile is deleted, delete bookmarks

        // Indexes for query performance
        // Primary query: Get bookmarks for a user, ordered by when they bookmarked
        builder.HasIndex(b => new { b.ProfileId, b.CreatedAt })
            .IsDescending(false, true) // ProfileId ASC, CreatedAt DESC
            .HasDatabaseName("IX_PulseBookmarks_ProfileId_CreatedAt");

        // Check if user has bookmarked a specific pulse (for isBookmarked flag)
        builder.HasIndex(b => new { b.ProfileId, b.PulseId })
            .IsUnique() // User can only bookmark a pulse once
            .HasDatabaseName("IX_PulseBookmarks_ProfileId_PulseId");
    }
}
