namespace Codewrinkles.Domain.Nova.Exceptions;

public sealed class ConversationNotFoundException : Exception
{
    public ConversationNotFoundException(Guid conversationId)
        : base($"Conversation with ID '{conversationId}' was not found.")
    {
        ConversationId = conversationId;
    }

    public Guid ConversationId { get; }
}
