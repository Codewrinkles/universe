using Codewrinkles.Domain.Nova;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Nova;

public sealed class AlphaApplicationConfiguration : IEntityTypeConfiguration<AlphaApplication>
{
    public void Configure(EntityTypeBuilder<AlphaApplication> builder)
    {
        // Table configuration - nova schema
        builder.ToTable("AlphaApplications", "nova");

        // Primary key
        builder.HasKey(a => a.Id);

        // Primary key value generation - EF Core will use sequential GUID generation
        builder.Property(a => a.Id)
            .ValueGeneratedOnAdd();

        // Applicant info
        builder.Property(a => a.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.PrimaryTechStack)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.YearsOfExperience)
            .IsRequired();

        builder.Property(a => a.Goal)
            .IsRequired()
            .HasMaxLength(2000);

        // Status
        builder.Property(a => a.Status)
            .IsRequired()
            .HasDefaultValue(AlphaApplicationStatus.Pending);

        // Timestamps
        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(a => a.ReviewedAt)
            .IsRequired(false);

        // Invite code
        builder.Property(a => a.InviteCode)
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(a => a.InviteCodeRedeemed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.RedeemedByProfileId)
            .IsRequired(false);

        builder.Property(a => a.RedeemedAt)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(a => a.Email)
            .HasDatabaseName("IX_AlphaApplications_Email");

        builder.HasIndex(a => a.InviteCode)
            .IsUnique()
            .HasFilter("[InviteCode] IS NOT NULL")
            .HasDatabaseName("IX_AlphaApplications_InviteCode");

        builder.HasIndex(a => a.Status)
            .HasDatabaseName("IX_AlphaApplications_Status");
    }
}
