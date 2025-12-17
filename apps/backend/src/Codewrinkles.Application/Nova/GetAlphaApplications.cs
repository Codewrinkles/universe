using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;
using System.Diagnostics;

namespace Codewrinkles.Application.Nova;

/// <summary>
/// Query to get all alpha applications for admin review
/// </summary>
public sealed record GetAlphaApplicationsQuery(AlphaApplicationStatus? StatusFilter = null) : ICommand<GetAlphaApplicationsResult>;

/// <summary>
/// Result containing list of alpha applications
/// </summary>
public sealed record GetAlphaApplicationsResult(IReadOnlyList<AlphaApplicationDto> Applications);

/// <summary>
/// DTO for alpha application
/// </summary>
public sealed record AlphaApplicationDto(
    Guid Id,
    string Email,
    string Name,
    string PrimaryTechStack,
    int YearsOfExperience,
    string Goal,
    AlphaApplicationStatus Status,
    string? InviteCode,
    bool InviteCodeRedeemed,
    DateTimeOffset CreatedAt);

/// <summary>
/// Handler for GetAlphaApplicationsQuery
/// </summary>
public sealed class GetAlphaApplicationsQueryHandler : ICommandHandler<GetAlphaApplicationsQuery, GetAlphaApplicationsResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAlphaApplicationsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetAlphaApplicationsResult> HandleAsync(
        GetAlphaApplicationsQuery query,
        CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity(SpanNames.Nova.GetAlphaApplications);

        IReadOnlyList<AlphaApplication> applications;

        if (query.StatusFilter.HasValue)
        {
            applications = await _unitOfWork.AlphaApplications
                .GetByStatusAsync(query.StatusFilter.Value, cancellationToken);
        }
        else
        {
            applications = await _unitOfWork.AlphaApplications
                .GetAllAsync(cancellationToken);
        }

        var dtos = applications
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AlphaApplicationDto(
                a.Id,
                a.Email,
                a.Name,
                a.PrimaryTechStack,
                a.YearsOfExperience,
                a.Goal,
                a.Status,
                a.InviteCode,
                a.InviteCodeRedeemed,
                a.CreatedAt))
            .ToList();

        return new GetAlphaApplicationsResult(dtos);
    }
}
