namespace Codewrinkles.Domain.Notification;

public enum NotificationType : byte
{
    PulseLike = 0,      // Someone liked your pulse
    PulseReply = 1,     // Someone replied to your pulse
    PulseRepulse = 2,   // Someone re-pulsed your pulse
    PulseMention = 3,   // Someone mentioned you in a pulse
    Follow = 4          // Someone followed you
}
