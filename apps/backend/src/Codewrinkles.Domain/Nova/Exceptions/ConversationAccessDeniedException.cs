namespace Codewrinkles.Domain.Nova.Exceptions;

public sealed class ConversationAccessDeniedException : Exception
{
    public ConversationAccessDeniedException(Guid conversationId, Guid profileId)
        : base($"Profile '{profileId}' does not have access to conversation '{conversationId}'.")
    {
        ConversationId = conversationId;
        ProfileId = profileId;
    }

    public Guid ConversationId { get; }
    public Guid ProfileId { get; }
}
