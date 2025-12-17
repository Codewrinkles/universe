using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;
using System.Diagnostics;

namespace Codewrinkles.Application.Nova;

/// <summary>
/// Command to add an alpha application to the waitlist
/// </summary>
public sealed record WaitlistAlphaApplicationCommand(Guid ApplicationId) : ICommand<WaitlistAlphaApplicationResult>;

/// <summary>
/// Result of waitlisting an alpha application
/// </summary>
public sealed record WaitlistAlphaApplicationResult(bool Success);

/// <summary>
/// Handler for WaitlistAlphaApplicationCommand
/// </summary>
public sealed class WaitlistAlphaApplicationCommandHandler : ICommandHandler<WaitlistAlphaApplicationCommand, WaitlistAlphaApplicationResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailQueue _emailQueue;

    public WaitlistAlphaApplicationCommandHandler(IUnitOfWork unitOfWork, IEmailQueue emailQueue)
    {
        _unitOfWork = unitOfWork;
        _emailQueue = emailQueue;
    }

    public async Task<WaitlistAlphaApplicationResult> HandleAsync(
        WaitlistAlphaApplicationCommand command,
        CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity(SpanNames.Nova.WaitlistAlphaApplication);

        // Find the application
        var application = await _unitOfWork.AlphaApplications
            .FindByIdAsync(command.ApplicationId, cancellationToken);

        if (application is null)
        {
            throw new AlphaApplicationNotFoundException(command.ApplicationId);
        }

        if (application.Status != Domain.Nova.AlphaApplicationStatus.Pending)
        {
            throw new AlphaApplicationNotPendingException(command.ApplicationId);
        }

        // Waitlist the application
        application.Waitlist();

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Queue waitlist email
        await _emailQueue.QueueAlphaWaitlistEmailAsync(
            application.Email,
            application.Name,
            cancellationToken);

        return new WaitlistAlphaApplicationResult(true);
    }
}
