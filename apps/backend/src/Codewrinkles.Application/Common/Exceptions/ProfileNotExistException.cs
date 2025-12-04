namespace Codewrinkles.Application.Common.Exceptions;

/// <summary>
/// Thrown when an operation references a profile that does not exist.
/// Used for validation in validators when checking profile existence.
/// </summary>
public sealed class ProfileNotExistException : Exception
{
    public ProfileNotExistException(Guid profileId)
        : base($"Profile with ID '{profileId}' does not exist.")
    {
        ProfileId = profileId;
    }

    public Guid ProfileId { get; }
}
