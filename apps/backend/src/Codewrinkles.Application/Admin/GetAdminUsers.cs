using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Telemetry;

namespace Codewrinkles.Application.Admin;

public sealed record GetAdminUsersQuery(
    int Page = 1,
    int PageSize = 20
) : ICommand<AdminUsersResponse>;

public sealed record AdminUserDto(
    Guid ProfileId,
    string Name,
    string? Handle,
    string? AvatarUrl,
    string Email,
    DateTimeOffset CreatedAt);

public sealed record AdminUsersResponse(
    IReadOnlyList<AdminUserDto> Users,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public sealed class GetAdminUsersQueryHandler
    : ICommandHandler<GetAdminUsersQuery, AdminUsersResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAdminUsersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminUsersResponse> HandleAsync(
        GetAdminUsersQuery query,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Admin.GetAdminUsers);

        try
        {
            var (profiles, totalCount) = await _unitOfWork.Profiles.GetAllForAdminAsync(
                query.Page,
                query.PageSize,
                cancellationToken);

            var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

            var users = profiles.Select(p => new AdminUserDto(
                ProfileId: p.Id,
                Name: p.Name,
                Handle: p.Handle,
                AvatarUrl: p.AvatarUrl,
                Email: p.Identity.Email,
                CreatedAt: p.CreatedAt
            )).ToList();

            activity?.SetTag("admin.users_count", users.Count);
            activity?.SetTag("admin.total_count", totalCount);
            activity?.SetSuccess(true);

            return new AdminUsersResponse(
                Users: users,
                TotalCount: totalCount,
                Page: query.Page,
                PageSize: query.PageSize,
                TotalPages: totalPages);
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}
