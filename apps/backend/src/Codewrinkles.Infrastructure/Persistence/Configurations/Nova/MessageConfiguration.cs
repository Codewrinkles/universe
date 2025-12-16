using Codewrinkles.Domain.Nova;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Nova;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        // Table configuration - nova schema
        builder.ToTable("Messages", "nova");

        // Primary key
        builder.HasKey(m => m.Id);

        // Primary key value generation - EF Core will use sequential GUID generation
        builder.Property(m => m.Id)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(m => m.Role)
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(m => m.Content)
            .HasMaxLength(Message.MaxContentLength)
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(m => m.TokensUsed)
            .IsRequired(false);

        builder.Property(m => m.ModelUsed)
            .HasMaxLength(100)
            .IsRequired(false);

        // Indexes
        // Composite index for fetching conversation messages in order
        builder.HasIndex(m => new { m.SessionId, m.CreatedAt })
            .HasDatabaseName("IX_Messages_SessionId_CreatedAt");
    }
}
