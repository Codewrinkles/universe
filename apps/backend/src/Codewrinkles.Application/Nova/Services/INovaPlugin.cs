namespace Codewrinkles.Application.Nova.Services;

/// <summary>
/// Marker interface for Nova plugins that can be used for tool calling.
/// Plugins implementing this interface will be automatically injected
/// into the SendMessageCommandHandler and passed to the LLM service.
/// </summary>
public interface INovaPlugin
{
}
