namespace Codewrinkles.Domain.Nova;

/// <summary>
/// The role of a message in a conversation.
/// </summary>
public enum MessageRole : byte
{
    /// <summary>
    /// Message from the user.
    /// </summary>
    User = 0,

    /// <summary>
    /// Message from the AI assistant (Nova).
    /// </summary>
    Assistant = 1,

    /// <summary>
    /// System message (e.g., coaching personality instructions).
    /// </summary>
    System = 2
}
