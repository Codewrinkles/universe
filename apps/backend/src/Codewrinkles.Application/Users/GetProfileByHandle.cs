using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Users;

public sealed record GetProfileByHandleQuery(
    string Handle
) : ICommand<ProfileDto>;

public sealed record ProfileDto(
    Guid ProfileId,
    string Name,
    string? Handle,
    string? Bio,
    string? AvatarUrl,
    string? Location,
    string? WebsiteUrl
);

public sealed class ProfileNotFoundByHandleException : Exception
{
    public ProfileNotFoundByHandleException(string handle)
        : base($"Profile with handle '{handle}' not found") { }
}

public sealed class GetProfileByHandleQueryHandler
    : ICommandHandler<GetProfileByHandleQuery, ProfileDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProfileByHandleQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProfileDto> HandleAsync(
        GetProfileByHandleQuery query,
        CancellationToken cancellationToken)
    {
        var profile = await _unitOfWork.Profiles.FindByHandleAsync(
            query.Handle,
            cancellationToken);

        if (profile is null)
        {
            throw new ProfileNotFoundByHandleException(query.Handle);
        }

        return new ProfileDto(
            ProfileId: profile.Id,
            Name: profile.Name,
            Handle: profile.Handle,
            Bio: profile.Bio,
            AvatarUrl: profile.AvatarUrl,
            Location: profile.Location,
            WebsiteUrl: profile.WebsiteUrl
        );
    }
}
