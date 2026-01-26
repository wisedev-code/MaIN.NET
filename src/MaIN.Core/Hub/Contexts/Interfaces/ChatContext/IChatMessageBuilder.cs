using MaIN.Domain.Entities;

namespace MaIN.Core.Hub.Contexts.Interfaces.ChatContext;

public interface IChatMessageBuilder : IChatActions
{
    /// <summary>
    /// Enables the visual output for the current chat session. This flag allows the AI to generate and return visual content,
    /// such as images or charts, as part of its response.
    /// </summary>
    /// <returns>The context instance implementing <see cref="IChatMessageBuilder"/> for method chaining.</returns>
    IChatMessageBuilder EnableVisual();
    
    /// <summary>
    /// Adds a user message to the chat. This method captures the message content and assigns the "User" role to it.
    /// It also timestamps the message for proper ordering.
    /// </summary>
    /// <param name="content">The message content that you wish to send.</param>
    /// <returns>The context instance implementing <see cref="IChatConfigurationBuilder"/> for method chaining.</returns>
    IChatConfigurationBuilder WithMessage(string content);
    
    /// <summary>
    /// Adds a message containing both text and image data.
    /// </summary>
    /// <param name="content">The text description or prompt.</param>
    /// <param name="image">The byte array containing image data.</param>
    /// <returns>The context instance implementing <see cref="IChatConfigurationBuilder"/> for method chaining.</returns>
    IChatConfigurationBuilder WithMessage(string content, byte[] image);
    
    /// <summary>
    /// Appends a collection of messages to the chat.
    /// </summary>
    /// <param name="messages">An enumerable list of <see cref="Message"/> objects.</param>
    /// <returns>The context instance implementing <see cref="IChatConfigurationBuilder"/> for method chaining.</returns>
    IChatConfigurationBuilder WithMessages(IEnumerable<Message> messages);
}