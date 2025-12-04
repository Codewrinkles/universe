namespace Codewrinkles.Application.Common.Exceptions;

/// <summary>
/// Thrown when a user attempts to modify a pulse they do not own.
/// </summary>
public sealed class UnauthorizedPulseAccessException : Exception
{
    public UnauthorizedPulseAccessException(Guid pulseId, Guid requestingProfileId)
        : base($"Profile '{requestingProfileId}' is not authorized to modify pulse '{pulseId}'.")
    {
        PulseId = pulseId;
        RequestingProfileId = requestingProfileId;
    }

    public Guid PulseId { get; }
    public Guid RequestingProfileId { get; }
}
