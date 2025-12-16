using Codewrinkles.Domain.Nova;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Nova;

public sealed class LearnerProfileConfiguration : IEntityTypeConfiguration<LearnerProfile>
{
    public void Configure(EntityTypeBuilder<LearnerProfile> builder)
    {
        // Table configuration - nova schema
        builder.ToTable("LearnerProfiles", "nova");

        // Primary key
        builder.HasKey(lp => lp.Id);

        // Primary key value generation - EF Core will use sequential GUID generation
        // This avoids index fragmentation issues with random GUIDs
        builder.Property(lp => lp.Id)
            .ValueGeneratedOnAdd();

        // Professional Background
        builder.Property(lp => lp.CurrentRole)
            .HasMaxLength(LearnerProfile.MaxRoleLength)
            .IsRequired(false);

        builder.Property(lp => lp.ExperienceYears)
            .IsRequired(false);

        builder.Property(lp => lp.PrimaryTechStack)
            .HasMaxLength(LearnerProfile.MaxTechStackLength)
            .IsRequired(false);

        builder.Property(lp => lp.CurrentProject)
            .HasMaxLength(LearnerProfile.MaxProjectLength)
            .IsRequired(false);

        // Learning Preferences
        builder.Property(lp => lp.LearningGoals)
            .HasMaxLength(LearnerProfile.MaxGoalsLength)
            .IsRequired(false);

        builder.Property(lp => lp.LearningStyle)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(lp => lp.PreferredPace)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired(false);

        // AI-Extracted Insights
        builder.Property(lp => lp.IdentifiedStrengths)
            .HasMaxLength(LearnerProfile.MaxInsightsLength)
            .IsRequired(false);

        builder.Property(lp => lp.IdentifiedStruggles)
            .HasMaxLength(LearnerProfile.MaxInsightsLength)
            .IsRequired(false);

        // Metadata
        builder.Property(lp => lp.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(lp => lp.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        // Relationships
        // One LearnerProfile per global Profile (1:1)
        builder.HasOne(lp => lp.Profile)
            .WithOne()
            .HasForeignKey<LearnerProfile>(lp => lp.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        // Unique index on ProfileId - each global profile can only have one learner profile
        builder.HasIndex(lp => lp.ProfileId)
            .IsUnique()
            .HasDatabaseName("IX_LearnerProfiles_ProfileId");
    }
}
