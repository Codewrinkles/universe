using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

public sealed record GetLearnerProfileQuery(
    Guid ProfileId
) : ICommand<GetLearnerProfileResult>;

public sealed record GetLearnerProfileResult(
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

public sealed class GetLearnerProfileQueryHandler
    : ICommandHandler<GetLearnerProfileQuery, GetLearnerProfileResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetLearnerProfileQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetLearnerProfileResult> HandleAsync(
        GetLearnerProfileQuery query,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Nova.GetLearnerProfile);
        activity?.SetProfileId(query.ProfileId);

        try
        {
            var learnerProfile = await _unitOfWork.Nova.FindLearnerProfileByProfileIdAsync(
                query.ProfileId,
                cancellationToken);

            // If no learner profile exists, create one automatically
            if (learnerProfile is null)
            {
                learnerProfile = LearnerProfile.Create(query.ProfileId);
                _unitOfWork.Nova.CreateLearnerProfile(learnerProfile);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            activity?.SetEntity("LearnerProfile", learnerProfile.Id);
            activity?.SetSuccess(true);

            return new GetLearnerProfileResult(
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
