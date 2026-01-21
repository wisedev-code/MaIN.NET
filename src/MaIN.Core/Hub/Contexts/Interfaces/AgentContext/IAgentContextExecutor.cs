using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Tools;
using MaIN.Domain.Models;
using MaIN.Services.Services.Models;

namespace MaIN.Core.Hub.Contexts.Interfaces.AgentContext;

public interface IAgentContextExecutor : IAgentActions
{
    /// <summary>
    /// Processes a chat through the agent, generating a response based on the chat's messages and the agent's context.
    /// </summary>
    /// <param name="chat">The <see cref="Message"/> object to process.</param>
    /// <param name="translate">A flag indicating whether the response should be translated.</param>
    /// <returns>A <see cref="ChatResult"/> object containing the processed message and other related information.</returns>
    Task<ChatResult> ProcessAsync(Chat chat, bool translate = false);

    /// <summary>
    /// Processes a user-provided message through the agent, generating a response based on the agent's context.
    /// </summary>
    /// <param name="message">The message to be processed by the agent.</param>
    /// <param name="translate">A flag indicating whether the response should be translated.</param>
    /// <param name="tokenCallback">Optional callback for receiving streaming tokens.</param>
    /// <param name="toolCallback">Optional callback for tool invocation events.</param>
    /// <returns>A <see cref="ChatResult"/> object containing the processed message and other related information.</returns>
    Task<ChatResult> ProcessAsync(string message, bool translate = false, Func<LLMTokenValue, Task>? tokenCallback = null,
        Func<ToolInvocation, Task>? toolCallback = null);

    /// <summary>
    /// Processes a message object through the agent, generating a response based on the agent's context and message data.
    /// </summary>
    /// <param name="message">The <see cref="Message"/> object to be processed.</param>
    /// <param name="translate">A flag indicating whether the response should be translated.</param>
    /// <param name="tokenCallback">Callback for streaming tokens.</param>
    /// <param name="toolCallback">Callback for tool invocations.</param>
    /// <returns>A <see cref="ChatResult"/> object containing the processed message and other related information.</returns>
    Task<ChatResult> ProcessAsync(Message message, bool translate = false, Func<LLMTokenValue, Task>? tokenCallback = null, 
        Func<ToolInvocation, Task>? toolCallback = null);

    /// <summary>
    /// Processes a collection of messages, updating the Agent's chat state.
    /// </summary>
    /// <param name="messages">A list of <see cref="Message"/> to process.</param>
    /// <param name="translate">Indicates if the result should be translated.</param>
    /// <param name="tokenCallback">Callback for streaming tokens.</param>
    /// <param name="toolCallback">Callback for tool invocations.</param>
    /// <returns>A <see cref="ChatResult"/> containing the model's response.</returns>
    Task<ChatResult> ProcessAsync(IEnumerable<Message> messages, bool translate = false, Func<LLMTokenValue, Task>? tokenCallback = null,
        Func<ToolInvocation, Task>? toolCallback = null);
}