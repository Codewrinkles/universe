using Codewrinkles.Domain.Nova;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Nova;

public sealed class ContentIngestionJobConfiguration : IEntityTypeConfiguration<ContentIngestionJob>
{
    public void Configure(EntityTypeBuilder<ContentIngestionJob> builder)
    {
        // Table configuration - nova schema
        builder.ToTable("ContentIngestionJobs", "nova");

        // Primary key
        builder.HasKey(j => j.Id);

        // Primary key value generation - EF Core will use sequential GUID generation
        // This avoids index fragmentation issues with random GUIDs
        builder.Property(j => j.Id)
            .ValueGeneratedOnAdd();

        // Required properties
        builder.Property(j => j.Source)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(j => j.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(j => j.Title)
            .IsRequired()
            .HasMaxLength(ContentIngestionJob.MaxTitleLength);

        builder.Property(j => j.ParentDocumentId)
            .IsRequired()
            .HasMaxLength(ContentIngestionJob.MaxParentDocumentIdLength);

        // Optional metadata properties
        builder.Property(j => j.Author)
            .HasMaxLength(ContentIngestionJob.MaxAuthorLength);

        builder.Property(j => j.Technology)
            .HasMaxLength(ContentIngestionJob.MaxTechnologyLength);

        builder.Property(j => j.SourceUrl)
            .HasMaxLength(ContentIngestionJob.MaxUrlLength);

        builder.Property(j => j.MaxPages)
            .IsRequired(false);

        // Progress tracking
        builder.Property(j => j.ChunksCreated)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(j => j.TotalPages)
            .IsRequired(false);

        builder.Property(j => j.PagesProcessed)
            .IsRequired(false);

        // Error handling
        builder.Property(j => j.ErrorMessage)
            .HasMaxLength(ContentIngestionJob.MaxErrorMessageLength);

        // Timestamps
        builder.Property(j => j.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(j => j.StartedAt)
            .IsRequired(false);

        builder.Property(j => j.CompletedAt)
            .IsRequired(false);

        // Indexes
        // For querying jobs by status
        builder.HasIndex(j => j.Status)
            .HasDatabaseName("IX_ContentIngestionJobs_Status");

        // For finding jobs by parent document ID
        builder.HasIndex(j => j.ParentDocumentId)
            .HasDatabaseName("IX_ContentIngestionJobs_ParentDocumentId");

        // For ordering by creation time
        builder.HasIndex(j => j.CreatedAt)
            .HasDatabaseName("IX_ContentIngestionJobs_CreatedAt");
    }
}
