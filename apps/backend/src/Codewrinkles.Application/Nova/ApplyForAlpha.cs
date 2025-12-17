using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

public sealed record ApplyForAlphaCommand(
    string Email,
    string Name,
    string PrimaryTechStack,
    int YearsOfExperience,
    string Goal
) : ICommand<ApplyForAlphaResult>;

public sealed record ApplyForAlphaResult(
    Guid ApplicationId,
    bool AlreadyApplied
);

public sealed class ApplyForAlphaCommandHandler
    : ICommandHandler<ApplyForAlphaCommand, ApplyForAlphaResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public ApplyForAlphaCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApplyForAlphaResult> HandleAsync(
        ApplyForAlphaCommand command,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Nova.ApplyForAlpha);
        activity?.SetEmailDomain(command.Email);

        try
        {
            // Check if already applied
            var existingApplication = await _unitOfWork.AlphaApplications
                .FindByEmailAsync(command.Email, cancellationToken);

            if (existingApplication is not null)
            {
                activity?.SetSuccess(true);
                activity?.SetTag("already_applied", "true");

                return new ApplyForAlphaResult(
                    ApplicationId: existingApplication.Id,
                    AlreadyApplied: true
                );
            }

            // Create new application
            var application = AlphaApplication.Create(
                email: command.Email,
                name: command.Name,
                primaryTechStack: command.PrimaryTechStack,
                yearsOfExperience: command.YearsOfExperience,
                goal: command.Goal
            );

            _unitOfWork.AlphaApplications.Create(application);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            activity?.SetSuccess(true);
            activity?.SetEntity("AlphaApplication", application.Id);

            return new ApplyForAlphaResult(
                ApplicationId: application.Id,
                AlreadyApplied: false
            );
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}
