using Codewrinkles.Domain.Identity;

namespace Codewrinkles.Domain.Pulse;

/// <summary>
/// Represents a user liking a pulse.
/// Composite primary key: (PulseId, ProfileId)
/// </summary>
public sealed class PulseLike
{
    // Composite Primary Key
    public Guid PulseId { get; private set; }
    public Guid ProfileId { get; private set; }

    // Timestamp
    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public Pulse Pulse { get; private set; }
    public Profile Profile { get; private set; }

    // Private parameterless constructor for EF Core
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
    private PulseLike() { }
#pragma warning restore CS8618

    /// <summary>
    /// Factory method to create a new pulse like.
    /// </summary>
    public static PulseLike Create(Guid pulseId, Guid profileId)
    {
        return new PulseLike
        {
            PulseId = pulseId,
            ProfileId = profileId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
