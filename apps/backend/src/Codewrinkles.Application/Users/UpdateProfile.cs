using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Users;

public sealed record UpdateProfileCommand(
    Guid ProfileId,
    string Name,
    string? Bio,
    string? Handle
) : ICommand<UpdateProfileResult>;

public sealed record UpdateProfileResult(
    Guid ProfileId,
    string Name,
    string? Handle,
    string? Bio,
    string? AvatarUrl
);

public sealed class ProfileNotFoundException : Exception
{
    public ProfileNotFoundException(Guid profileId)
        : base($"Profile with ID {profileId} not found") { }
}

public sealed class HandleAlreadyTakenException : Exception
{
    public HandleAlreadyTakenException(string handle)
        : base($"Handle '{handle}' is already taken") { }
}

public sealed class UpdateProfileCommandHandler
    : ICommandHandler<UpdateProfileCommand, UpdateProfileResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProfileCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateProfileResult> HandleAsync(
        UpdateProfileCommand command,
        CancellationToken cancellationToken)
    {
        // Validator has already confirmed:
        // - Profile exists
        // - Handle is unique (if being changed)

        // 1. Find the profile (guaranteed to exist after validation)
        var profile = (await _unitOfWork.Profiles.FindByIdAsync(
            command.ProfileId,
            cancellationToken))!;

        // 2. Update the profile
        profile.UpdateProfileDetails(
            name: command.Name,
            bio: command.Bio,
            handle: command.Handle);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 3. Return result
        return new UpdateProfileResult(
            ProfileId: profile.Id,
            Name: profile.Name,
            Handle: profile.Handle,
            Bio: profile.Bio,
            AvatarUrl: profile.AvatarUrl
        );
    }
}
