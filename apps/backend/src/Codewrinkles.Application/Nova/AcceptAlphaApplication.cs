using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;
using System.Diagnostics;

namespace Codewrinkles.Application.Nova;

/// <summary>
/// Command to accept an alpha application and generate an invite code
/// </summary>
public sealed record AcceptAlphaApplicationCommand(Guid ApplicationId) : ICommand<AcceptAlphaApplicationResult>;

/// <summary>
/// Result of accepting an alpha application
/// </summary>
public sealed record AcceptAlphaApplicationResult(string InviteCode);

/// <summary>
/// Exception thrown when application is not found
/// </summary>
public sealed class AlphaApplicationNotFoundException : Exception
{
    public AlphaApplicationNotFoundException(Guid applicationId)
        : base($"Alpha application with ID '{applicationId}' was not found")
    {
        ApplicationId = applicationId;
    }

    public Guid ApplicationId { get; }
}

/// <summary>
/// Exception thrown when application is not in pending status
/// </summary>
public sealed class AlphaApplicationNotPendingException : Exception
{
    public AlphaApplicationNotPendingException(Guid applicationId)
        : base($"Alpha application with ID '{applicationId}' is not in pending status")
    {
        ApplicationId = applicationId;
    }

    public Guid ApplicationId { get; }
}

/// <summary>
/// Handler for AcceptAlphaApplicationCommand
/// </summary>
public sealed class AcceptAlphaApplicationCommandHandler : ICommandHandler<AcceptAlphaApplicationCommand, AcceptAlphaApplicationResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailQueue _emailQueue;

    public AcceptAlphaApplicationCommandHandler(IUnitOfWork unitOfWork, IEmailQueue emailQueue)
    {
        _unitOfWork = unitOfWork;
        _emailQueue = emailQueue;
    }

    public async Task<AcceptAlphaApplicationResult> HandleAsync(
        AcceptAlphaApplicationCommand command,
        CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity(SpanNames.Nova.AcceptAlphaApplication);

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

        // Accept the application (generates invite code)
        application.Accept();

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Queue acceptance email with invite code
        await _emailQueue.QueueAlphaAcceptanceEmailAsync(
            application.Email,
            application.Name,
            application.InviteCode!,
            cancellationToken);

        return new AcceptAlphaApplicationResult(application.InviteCode!);
    }
}
