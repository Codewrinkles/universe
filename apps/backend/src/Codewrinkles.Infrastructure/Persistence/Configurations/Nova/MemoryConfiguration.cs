using Codewrinkles.Domain.Nova;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Nova;

public sealed class MemoryConfiguration : IEntityTypeConfiguration<Memory>
{
    public void Configure(EntityTypeBuilder<Memory> builder)
    {
        // Table configuration - nova schema
        builder.ToTable("Memories", "nova");

        // Primary key
        builder.HasKey(m => m.Id);

        // Primary key value generation - EF Core will use sequential GUID generation
        // This avoids index fragmentation issues with random GUIDs
        builder.Property(m => m.Id)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(m => m.Category)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.Content)
            .HasMaxLength(Memory.MaxContentLength)
            .IsRequired();

        // Embedding stored as varbinary(max) - no explicit max length needed
        builder.Property(m => m.Embedding)
            .IsRequired(false);

        builder.Property(m => m.Importance)
            .IsRequired()
            .HasDefaultValue(Memory.DefaultImportance);

        builder.Property(m => m.OccurrenceCount)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(m => m.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(m => m.SupersededAt)
            .IsRequired(false);

        builder.Property(m => m.SupersededById)
            .IsRequired(false);

        // Relationships
        builder.HasOne(m => m.Profile)
            .WithMany()
            .HasForeignKey(m => m.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.SourceSession)
            .WithMany()
            .HasForeignKey(m => m.SourceSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referential relationship for superseded memories
        builder.HasOne(m => m.SupersededBy)
            .WithMany()
            .HasForeignKey(m => m.SupersededById)
            .OnDelete(DeleteBehavior.NoAction);

        // Indexes
        // For querying active memories by category
        builder.HasIndex(m => new { m.ProfileId, m.Category, m.SupersededAt })
            .HasDatabaseName("IX_Memories_ProfileId_Category_SupersededAt")
            .HasFilter("[SupersededAt] IS NULL");

        // For recent memories query
        builder.HasIndex(m => new { m.ProfileId, m.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_Memories_ProfileId_CreatedAt_Desc");

        // For high-importance memories query
        builder.HasIndex(m => new { m.ProfileId, m.Importance })
            .IsDescending(false, true)
            .HasDatabaseName("IX_Memories_ProfileId_Importance_Desc");

        // For finding memories from a session
        builder.HasIndex(m => m.SourceSessionId)
            .HasDatabaseName("IX_Memories_SourceSessionId");
    }
}
