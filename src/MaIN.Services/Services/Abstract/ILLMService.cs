using MaIN.Domain.Entities;
using MaIN.Services.Services.LLMService;
using MaIN.Services.Services.Models;

namespace MaIN.Services.Services.Abstract;

/// <summary>
/// Service responsible for interacting with the LLM provider
/// </summary>
public interface ILLMService
{
    /// <summary>
    /// Sends a chat (with history) to the LLM provider.
    /// </summary>
    /// <param name="chat">Chat enetity</param>
    /// <param name="requestOptions">Extensions that aims to tweak response to user needs</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ChatResult?> Send(Chat chat,
        ChatRequestOptions requestOptions, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sending chat interference request to retrieve memory data
    /// </summary>
    /// <param name="chat">Chat entity</param>
    /// <param name="memoryOptions">Memory specific interference params</param>
    /// <param name="requestOptions"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ChatResult?> AskMemory(Chat chat,
        ChatMemoryOptions memoryOptions,
        ChatRequestOptions requestOptions,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get current models available in the LLM provider
    /// </summary>
    /// <returns></returns>
    Task<string[]> GetCurrentModels();
    
    /// <summary>
    /// Cleanup of unused chats/agents sessions
    /// </summary>
    /// <param name="id">Id of chat to be cleaned</param>
    /// <returns></returns>
    Task CleanSessionCache(string id);
}