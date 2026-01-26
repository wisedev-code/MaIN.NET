using MaIN.Domain.Entities;

namespace MaIN.Core.Hub.Contexts.Interfaces.ChatContext;

public interface IChatActions
{
    /// <summary>
    /// Returns the unique identifier of the current chat session. This is useful for tracking or managing specific chats
    /// within a broader system.
    /// </summary>
    /// <returns>A string representing the chat's unique identifier.</returns>
    string GetChatId();

    /// <summary>
    /// Retrieves the current chat session by its ID. This method is useful when you need to access the ongoing chat session
    /// and inspect its data.
    /// </summary>
    /// <returns>A <see cref="Chat"/> object representing the current chat session.</returns>
    Task<Chat> GetCurrentChat();

    /// <summary>
    /// Fetches all available chat sessions stored in the system. This can be used to list past chat sessions.
    /// </summary>
    /// <returns>A list of <see cref="Chat"/> objects representing all chat sessions.</returns>
    Task<List<Chat>> GetAllChats();

    /// <summary>
    /// Deletes the current chat session. This is useful for cleanup or when you no longer need the chat data.
    /// </summary>
    Task DeleteChat();

    /// <summary>
    /// Retrieves a simplified list of message summaries from the chat history. This is useful for viewing a short overview
    /// of the conversation without the full message details.
    /// </summary>
    /// <returns>A list of <see cref="MessageShort"/> objects, each containing the content, role (user/system),
    /// and timestamp of a message.</returns>
    List<MessageShort> GetChatHistory();
}