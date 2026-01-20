namespace MaIN.Core.Hub.Contexts.Interfaces.ChatContext;

public interface IChatBuilderEntryPoint : IChatActions
{
    /// <summary>
    /// Sets the standard model to be used for the chat session.
    /// </summary>
    /// <param name="model">The name or identifier of the LLM model.</param>
    IChatMessageBuilder WithModel(string model);
    
    /// <summary>
    /// Configures a custom model with a specific path and project context.
    /// </summary>
    /// <param name="model">The name of the custom model.</param>
    /// <param name="path">The path to the model files.</param>
    /// <param name="mmProject">Optional multi-modal project identifier.</param>
    
    IChatMessageBuilder WithCustomModel(string model, string path, string? mmProject = null);
    /// <summary>
    /// Enables visual/image generation mode. Use this method now if you do not plan to explicitly define the model.
    /// Otherwise, you will be able to use this method after defining the model.
    /// </summary>
    IChatMessageBuilder EnableVisual();
    
    /// <summary>
    /// Loads an existing chat session from the database using its unique identifier.
    /// </summary>
    /// <param name="chatId">The GUID of the existing chat.</param>
    Task<IChatConfigurationBuilder> FromExisting(string chatId);
}