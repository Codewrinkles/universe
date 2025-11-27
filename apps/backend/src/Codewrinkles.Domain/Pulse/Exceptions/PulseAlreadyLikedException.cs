namespace Codewrinkles.Domain.Pulse.Exceptions;

public sealed class PulseAlreadyLikedException : Exception
{
    public PulseAlreadyLikedException(Guid pulseId, Guid profileId)
        : base($"Pulse '{pulseId}' has already been liked by profile '{profileId}'.")
    {
        PulseId = pulseId;
        ProfileId = profileId;
    }

    public Guid PulseId { get; }
    public Guid ProfileId { get; }
}
