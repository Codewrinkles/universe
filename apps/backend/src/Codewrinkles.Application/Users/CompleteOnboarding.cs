using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Users;

public sealed record CompleteOnboardingCommand(Guid ProfileId) : ICommand<CompleteOnboardingResult>;

public sealed record CompleteOnboardingResult(bool Success);

public sealed class CompleteOnboardingCommandHandler
    : ICommandHandler<CompleteOnboardingCommand, CompleteOnboardingResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public CompleteOnboardingCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CompleteOnboardingResult> HandleAsync(
        CompleteOnboardingCommand command,
        CancellationToken cancellationToken)
    {
        var profile = await _unitOfWork.Profiles.FindByIdAsync(
            command.ProfileId,
            cancellationToken);

        if (profile == null)
            throw new ProfileNotFoundException(command.ProfileId);

        profile.CompleteOnboarding();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CompleteOnboardingResult(Success: true);
    }
}
