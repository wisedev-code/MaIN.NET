namespace MaIN.Core.Hub.Contexts.Interfaces.ChatContext;

public interface IChatBuilderEntryPoint : IChatActions
{
    /// <summary>
    /// Sets the AI model to be used for the current chat session. This determines how the AI will respond to messages
    /// based on the selected model.
    /// </summary>
    /// <param name="model">The ID of the AI model to be used.</param>
    /// <returns>The context instance implementing <see cref="IChatMessageBuilder"/> for method chaining.</returns>
    IChatMessageBuilder WithModel(string model);

    /// <summary>
    /// Loads an existing chat session from the database using its unique identifier.
    /// </summary>
    /// <param name="chatId">The GUID of the existing chat.</param>
    /// <returns>The context instance implementing <see cref="IChatConfigurationBuilder"/> for method chaining.</returns>
    Task<IChatConfigurationBuilder> FromExisting(string chatId);
}
