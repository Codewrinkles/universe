namespace Codewrinkles.Domain.Pulse.Exceptions;

public sealed class PulseNotFoundException : Exception
{
    public PulseNotFoundException(Guid pulseId)
        : base($"Pulse with ID '{pulseId}' was not found.")
    {
        PulseId = pulseId;
    }

    public Guid PulseId { get; }
}
