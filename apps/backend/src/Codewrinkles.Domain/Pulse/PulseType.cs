namespace Codewrinkles.Domain.Pulse;

public enum PulseType : byte
{
    Original = 0,    // Standard pulse
    Repulse = 1,     // Quote/re-pulse of another pulse
    Reply = 2        // Reply to another pulse (future)
}
