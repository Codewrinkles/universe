namespace Codewrinkles.Domain.Pulse;

public sealed class PulseHashtag
{
    // Properties
    public Guid PulseId { get; private set; }
    public Guid HashtagId { get; private set; }
    public int Position { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public Pulse Pulse { get; private set; }
    public Hashtag Hashtag { get; private set; }

    // Private parameterless constructor for EF Core materialization only
    // EF Core will populate all properties via reflection when loading from database
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
    private PulseHashtag() { }
#pragma warning restore CS8618

    // Factory method for creating valid instances
    public static PulseHashtag Create(Guid pulseId, Guid hashtagId, int position)
    {
        return new PulseHashtag
        {
            PulseId = pulseId,
            HashtagId = hashtagId,
            Position = position,
            CreatedAt = DateTime.UtcNow
        };
    }
}
