namespace Codewrinkles.Domain.Pulse;

/// <summary>
/// Represents a mention of a user in a pulse
/// Stores mention relationships for notifications and analytics
/// </summary>
public sealed class PulseMention
{
    // Properties
    public Guid PulseId { get; private set; }
    public Guid ProfileId { get; private set; }
    public string Handle { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public Pulse Pulse { get; private set; }
    public Identity.Profile MentionedProfile { get; private set; }

    // Private parameterless constructor for EF Core materialization only
    // EF Core will populate all properties via reflection when loading from database
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
    private PulseMention() { }
#pragma warning restore CS8618

    // Factory method
    public static PulseMention Create(Guid pulseId, Guid profileId, string handle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(handle);

        return new PulseMention
        {
            PulseId = pulseId,
            ProfileId = profileId,
            Handle = handle.Trim().ToLowerInvariant(),
            CreatedAt = DateTime.UtcNow
        };
    }
}
