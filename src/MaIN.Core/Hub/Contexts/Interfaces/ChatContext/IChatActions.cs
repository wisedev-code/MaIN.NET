using MaIN.Domain.Entities;

namespace MaIN.Core.Hub.Contexts.Interfaces.ChatContext;

public interface IChatActions
{
    /// <summary>
    /// Gets the unique identifier (GUID) of the current chat session.
    /// </summary>
    string GetChatId();
    
    /// <summary>
    /// Retrieves the full chat object with all its messages and properties.
    /// </summary>
    Task<Chat> GetCurrentChat();
    
    /// <summary>
    /// Lists all chat sessions stored in the system.
    /// </summary>
    Task<List<Chat>> GetAllChats();
    
    /// <summary>
    /// Permanently deletes the current chat session and its history.
    /// </summary>
    Task DeleteChat();
    
    /// <summary>
    /// Provides a lightweight summary of the chat history (Role, Content, Time).
    /// </summary>
    List<MessageShort> GetChatHistory();
}