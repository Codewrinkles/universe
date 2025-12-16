using Codewrinkles.Domain.Nova;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Nova;

public sealed class ConversationSessionConfiguration : IEntityTypeConfiguration<ConversationSession>
{
    public void Configure(EntityTypeBuilder<ConversationSession> builder)
    {
        // Table configuration - nova schema
        builder.ToTable("ConversationSessions", "nova");

        // Primary key
        builder.HasKey(c => c.Id);

        // Primary key value generation - EF Core will use sequential GUID generation
        // This avoids index fragmentation issues with random GUIDs
        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(c => c.Title)
            .HasMaxLength(ConversationSession.MaxTitleLength)
            .IsRequired(false);

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(c => c.LastMessageAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(c => c.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Memory extraction tracking
        builder.Property(c => c.LastMemoryExtractionAt)
            .IsRequired(false);

        builder.Property(c => c.LastProcessedMessageId)
            .IsRequired(false);

        // Relationships
        builder.HasOne(c => c.Owner)
            .WithMany()
            .HasForeignKey(c => c.ProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Session)
            .HasForeignKey(m => m.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        // Composite index for listing user's conversations - most common query
        builder.HasIndex(c => new { c.ProfileId, c.IsDeleted, c.LastMessageAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("IX_ConversationSessions_ProfileId_IsDeleted_LastMessageAt");

        // Single-column indexes for specific queries
        builder.HasIndex(c => c.ProfileId)
            .HasDatabaseName("IX_ConversationSessions_ProfileId");

        builder.HasIndex(c => c.IsDeleted)
            .HasDatabaseName("IX_ConversationSessions_IsDeleted");

        // Index for finding sessions needing memory extraction
        builder.HasIndex(c => new { c.ProfileId, c.LastMemoryExtractionAt })
            .HasDatabaseName("IX_ConversationSessions_ProfileId_LastMemoryExtractionAt");
    }
}
