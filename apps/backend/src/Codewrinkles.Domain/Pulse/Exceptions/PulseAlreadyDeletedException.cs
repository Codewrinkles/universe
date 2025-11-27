namespace Codewrinkles.Domain.Pulse.Exceptions;

public sealed class PulseAlreadyDeletedException : Exception
{
    public PulseAlreadyDeletedException(Guid pulseId)
        : base($"Pulse '{pulseId}' has already been deleted.")
    {
        PulseId = pulseId;
    }

    public Guid PulseId { get; }
}
