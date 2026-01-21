namespace MaIN.Core.Hub.Contexts.Interfaces.ChatContext;

public interface IChatBuilderEntryPoint : IChatActions
{
    /// <summary>
    /// Sets the AI model to be used for the current chat session. This determines how the AI will respond to messages
    /// based on the selected model.
    /// </summary>
    /// <param name="model">The name of the AI model to be used.</param>
    /// <returns>The context instance implementing <see cref="IChatMessageBuilder"/> for method chaining.</returns>
    IChatMessageBuilder WithModel(string model);
    
    /// <summary>
    /// Configures a custom model with a specific path and project context.
    /// </summary>
    /// <param name="model">The name of the custom model.</param>
    /// <param name="path">The path to the model files.</param>
    /// <param name="mmProject">Optional multi-modal project identifier.</param>
    /// <returns>The context instance implementing <see cref="IChatMessageBuilder"/> for method chaining.</returns>
    IChatMessageBuilder WithCustomModel(string model, string path, string? mmProject = null);
    
    /// <summary>
    /// Enables visual/image generation mode. Use this method now if you do not plan to explicitly define the model.
    /// Otherwise, you will be able to use this method in the next step, after defining the model.
    /// </summary>
    /// <returns>The context instance implementing <see cref="IChatMessageBuilder"/> for method chaining.</returns>
    IChatMessageBuilder EnableVisual();
    
    /// <summary>
    /// Loads an existing chat session from the database using its unique identifier.
    /// </summary>
    /// <param name="chatId">The GUID of the existing chat.</param>
    /// <returns>The context instance implementing <see cref="IChatConfigurationBuilder"/> for method chaining.</returns>
    Task<IChatConfigurationBuilder> FromExisting(string chatId);
}