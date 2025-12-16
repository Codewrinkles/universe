using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

public sealed record UpdateLearnerProfileCommand(
    Guid ProfileId,
    string? CurrentRole,
    int? ExperienceYears,
    string? PrimaryTechStack,
    string? CurrentProject,
    string? LearningGoals,
    string? LearningStyle,
    string? PreferredPace
) : ICommand<UpdateLearnerProfileResult>;

public sealed record UpdateLearnerProfileResult(
    Guid Id,
    Guid ProfileId,
    string? CurrentRole,
    int? ExperienceYears,
    string? PrimaryTechStack,
    string? CurrentProject,
    string? LearningGoals,
    string? LearningStyle,
    string? PreferredPace,
    string? IdentifiedStrengths,
    string? IdentifiedStruggles,
    bool HasUserData,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public sealed class UpdateLearnerProfileCommandHandler
    : ICommandHandler<UpdateLearnerProfileCommand, UpdateLearnerProfileResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateLearnerProfileCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateLearnerProfileResult> HandleAsync(
        UpdateLearnerProfileCommand command,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Nova.UpdateLearnerProfile);
        activity?.SetProfileId(command.ProfileId);

        try
        {
            var learnerProfile = await _unitOfWork.Nova.FindLearnerProfileByProfileIdAsync(
                command.ProfileId,
                cancellationToken);

            // If no learner profile exists, create one
            if (learnerProfile is null)
            {
                learnerProfile = LearnerProfile.Create(command.ProfileId);
                _unitOfWork.Nova.CreateLearnerProfile(learnerProfile);
            }

            // Parse enums from string values
            LearningStyle? learningStyle = null;
            if (!string.IsNullOrWhiteSpace(command.LearningStyle) &&
                Enum.TryParse<LearningStyle>(command.LearningStyle, ignoreCase: true, out var parsedStyle))
            {
                learningStyle = parsedStyle;
            }

            PreferredPace? preferredPace = null;
            if (!string.IsNullOrWhiteSpace(command.PreferredPace) &&
                Enum.TryParse<PreferredPace>(command.PreferredPace, ignoreCase: true, out var parsedPace))
            {
                preferredPace = parsedPace;
            }

            // Update the profile
            learnerProfile.UpdateAll(
                currentRole: command.CurrentRole,
                experienceYears: command.ExperienceYears,
                primaryTechStack: command.PrimaryTechStack,
                currentProject: command.CurrentProject,
                learningGoals: command.LearningGoals,
                learningStyle: learningStyle,
                preferredPace: preferredPace);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            activity?.SetEntity("LearnerProfile", learnerProfile.Id);
            activity?.SetSuccess(true);

            return new UpdateLearnerProfileResult(
                Id: learnerProfile.Id,
                ProfileId: learnerProfile.ProfileId,
                CurrentRole: learnerProfile.CurrentRole,
                ExperienceYears: learnerProfile.ExperienceYears,
                PrimaryTechStack: learnerProfile.PrimaryTechStack,
                CurrentProject: learnerProfile.CurrentProject,
                LearningGoals: learnerProfile.LearningGoals,
                LearningStyle: learnerProfile.LearningStyle?.ToString(),
                PreferredPace: learnerProfile.PreferredPace?.ToString(),
                IdentifiedStrengths: learnerProfile.IdentifiedStrengths,
                IdentifiedStruggles: learnerProfile.IdentifiedStruggles,
                HasUserData: learnerProfile.HasUserData(),
                CreatedAt: learnerProfile.CreatedAt,
                UpdatedAt: learnerProfile.UpdatedAt);
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}
