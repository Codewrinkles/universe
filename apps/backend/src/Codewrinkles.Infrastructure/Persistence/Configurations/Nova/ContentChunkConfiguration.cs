using Codewrinkles.Domain.Nova;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Nova;

public sealed class ContentChunkConfiguration : IEntityTypeConfiguration<ContentChunk>
{
    public void Configure(EntityTypeBuilder<ContentChunk> builder)
    {
        // Table configuration - nova schema
        builder.ToTable("ContentChunks", "nova");

        // Primary key
        builder.HasKey(c => c.Id);

        // Primary key value generation - EF Core will use sequential GUID generation
        // This avoids index fragmentation issues with random GUIDs
        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        // Required properties
        builder.Property(c => c.Source)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(c => c.SourceIdentifier)
            .IsRequired()
            .HasMaxLength(ContentChunk.MaxSourceIdentifierLength);

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(ContentChunk.MaxTitleLength);

        builder.Property(c => c.Content)
            .IsRequired()
            .HasMaxLength(ContentChunk.MaxContentLength);

        // Embedding stored as varbinary(max) - no explicit max length needed
        builder.Property(c => c.Embedding)
            .IsRequired();

        builder.Property(c => c.TokenCount)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(c => c.UpdatedAt)
            .IsRequired(false);

        // Optional metadata properties
        builder.Property(c => c.Author)
            .HasMaxLength(ContentChunk.MaxAuthorLength);

        builder.Property(c => c.Technology)
            .HasMaxLength(ContentChunk.MaxTechnologyLength);

        builder.Property(c => c.ChunkIndex)
            .IsRequired(false);

        builder.Property(c => c.ParentDocumentId)
            .HasMaxLength(ContentChunk.MaxParentDocumentIdLength);

        builder.Property(c => c.PublishedAt)
            .IsRequired(false);

        builder.Property(c => c.StartTime)
            .IsRequired(false);

        builder.Property(c => c.EndTime)
            .IsRequired(false);

        builder.Property(c => c.SectionPath)
            .HasMaxLength(ContentChunk.MaxSectionPathLength);

        // Indexes
        // For filtering by source type
        builder.HasIndex(c => c.Source)
            .HasDatabaseName("IX_ContentChunks_Source");

        // For filtering by technology
        builder.HasIndex(c => c.Technology)
            .HasDatabaseName("IX_ContentChunks_Technology");

        // For filtering by author
        builder.HasIndex(c => c.Author)
            .HasDatabaseName("IX_ContentChunks_Author");

        // Unique index for deduplication
        builder.HasIndex(c => new { c.Source, c.SourceIdentifier })
            .IsUnique()
            .HasDatabaseName("IX_ContentChunks_Source_SourceIdentifier");

        // For grouping chunks from the same parent document
        builder.HasIndex(c => c.ParentDocumentId)
            .HasDatabaseName("IX_ContentChunks_ParentDocumentId");
    }
}
