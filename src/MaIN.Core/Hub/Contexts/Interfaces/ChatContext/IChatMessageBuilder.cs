using MaIN.Domain.Entities;

namespace MaIN.Core.Hub.Contexts.Interfaces.ChatContext;

public interface IChatMessageBuilder : IChatActions
{
    /// <summary>
    /// Enables visual/image generation mode.
    /// </summary>
    IChatMessageBuilder EnableVisual();
    
    /// <summary>
    /// Adds a single text message to the chat context.
    /// </summary>
    /// <param name="content">The text content of the message.</param>
    IChatCompletionBuilder WithMessage(string content);
    
    /// <summary>
    /// Adds a message containing both text and image data.
    /// </summary>
    /// <param name="content">The text description or prompt.</param>
    /// <param name="image">The byte array containing image data.</param>
    IChatCompletionBuilder WithMessage(string content, byte[] image);
    
    /// <summary>
    /// Appends a collection of messages to the chat.
    /// </summary>
    /// <param name="messages">An enumerable list of message objects.</param>
    IChatCompletionBuilder WithMessages(IEnumerable<Message> messages);
}