using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Users;

public sealed record UploadAvatarCommand(
    Guid ProfileId,
    Stream ImageStream
) : ICommand<UploadAvatarResult>;

public sealed record UploadAvatarResult(
    Guid ProfileId,
    string AvatarUrl
);

public sealed class InvalidImageException : Exception
{
    public InvalidImageException(string message) : base(message) { }
}

public sealed class UploadAvatarCommandHandler
    : ICommandHandler<UploadAvatarCommand, UploadAvatarResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAvatarService _avatarService;

    public UploadAvatarCommandHandler(
        IUnitOfWork unitOfWork,
        IAvatarService avatarService)
    {
        _unitOfWork = unitOfWork;
        _avatarService = avatarService;
    }

    public async Task<UploadAvatarResult> HandleAsync(
        UploadAvatarCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Find the profile
        var profile = await _unitOfWork.Profiles.FindByIdAsync(
            command.ProfileId,
            cancellationToken);

        if (profile is null)
        {
            throw new ProfileNotFoundException(command.ProfileId);
        }

        // 2. Process and save the avatar image
        string avatarUrl;
        try
        {
            avatarUrl = await _avatarService.SaveAvatarAsync(
                command.ImageStream,
                command.ProfileId,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not ProfileNotFoundException)
        {
            throw new InvalidImageException(
                "Failed to process image. Please upload a valid image file (JPEG, PNG, GIF, or WebP).");
        }

        // 3. Update profile with new avatar URL
        profile.UpdateAvatarUrl(avatarUrl);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Return result
        return new UploadAvatarResult(
            ProfileId: profile.Id,
            AvatarUrl: avatarUrl
        );
    }
}
