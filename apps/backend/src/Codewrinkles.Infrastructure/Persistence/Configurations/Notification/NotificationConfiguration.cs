using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Codewrinkles.Domain.Notification;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Notification;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Domain.Notification.Notification>
{
    public void Configure(EntityTypeBuilder<Domain.Notification.Notification> builder)
    {
        builder.ToTable("Notifications", "notification");

        builder.HasKey(n => n.Id);

        // Explicitly configure sequential GUID generation
        // This avoids index fragmentation issues with random GUIDs
        builder.Property(n => n.Id)
            .ValueGeneratedOnAdd();

        builder.Property(n => n.RecipientId)
            .IsRequired();

        builder.Property(n => n.ActorId)
            .IsRequired();

        builder.Property(n => n.Type)
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(n => n.EntityId)
            .IsRequired(false);

        builder.Property(n => n.EntityType)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(n => n.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        builder.Property(n => n.ReadAt)
            .IsRequired(false);

        // Relationships
        builder.HasOne(n => n.Recipient)
            .WithMany()
            .HasForeignKey(n => n.RecipientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Actor)
            .WithMany()
            .HasForeignKey(n => n.ActorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for query performance
        // Primary query: Get unread notifications for a user, ordered by date
        builder.HasIndex(n => new { n.RecipientId, n.CreatedAt })
            .IsDescending(false, true) // RecipientId ASC, CreatedAt DESC
            .HasDatabaseName("IX_Notifications_RecipientId_CreatedAt");

        // Filtered index for unread notifications (significantly reduces index size)
        builder.HasIndex(n => new { n.RecipientId, n.IsRead })
            .HasFilter("[IsRead] = 0")
            .HasDatabaseName("IX_Notifications_RecipientId_IsRead_Unread");
    }
}
