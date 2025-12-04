namespace Codewrinkles.Application.Common.Exceptions;

/// <summary>
/// Thrown when attempting to unlike a pulse that the user has not liked.
/// </summary>
public sealed class PulseNotLikedException : Exception
{
    public PulseNotLikedException(Guid pulseId, Guid profileId)
        : base($"Profile '{profileId}' has not liked pulse '{pulseId}'.")
    {
        PulseId = pulseId;
        ProfileId = profileId;
    }

    public Guid PulseId { get; }
    public Guid ProfileId { get; }
}
