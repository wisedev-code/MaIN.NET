using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Tools;
using MaIN.Domain.Models;
using MaIN.Services.Services.Models;

namespace MaIN.Core.Hub.Contexts.Interfaces.AgentContext;

public interface IAgentContextExecutor : IAgentActions
{
    /// <summary>
    /// Processes a request based on a full chat object including history.
    /// </summary>
    /// <param name="chat">The chat object with history.</param>
    /// <param name="translate">Indicates if the result should be translated.</param>
    /// <returns>A <see cref="ChatResult"/> containing the model's response.</returns>
    Task<ChatResult> ProcessAsync(Chat chat, bool translate = false);

    /// <summary>
    /// Processes a simple text message from the user.
    /// </summary>
    /// <param name="message">The text content of the message.</param>
    /// <param name="translate">Indicates if the result should be translated.</param>
    /// <param name="tokenCallback">Optional callback for receiving streaming tokens.</param>
    /// <param name="toolCallback">Optional callback for tool invocation events.</param>
    /// <returns>A <see cref="ChatResult"/> containing the model's response.</returns>
    Task<ChatResult> ProcessAsync(string message, bool translate = false, Func<LLMTokenValue, Task>? tokenCallback = null, Func<ToolInvocation, Task>? toolCallback = null);

    /// <summary>
    /// Processes a single message object (may include images or files).
    /// </summary>
    /// <param name="message">The <see cref="Message"/> object.</param>
    /// <param name="translate">Indicates if the result should be translated.</param>
    /// <param name="tokenCallback">Callback for streaming tokens.</param>
    /// <param name="toolCallback">Callback for tool invocations.</param>
    /// <returns>A <see cref="ChatResult"/> containing the model's response.</returns>
    Task<ChatResult> ProcessAsync(Message message, bool translate = false, Func<LLMTokenValue, Task>? tokenCallback = null, Func<ToolInvocation, Task>? toolCallback = null);

    /// <summary>
    /// Processes a collection of messages, updating the Agent's chat state.
    /// </summary>
    /// <param name="messages">A list of messages to process.</param>
    /// <param name="translate">Indicates if the result should be translated.</param>
    /// <param name="tokenCallback">Callback for streaming tokens.</param>
    /// <param name="toolCallback">Callback for tool invocations.</param>
    /// <returns>A <see cref="ChatResult"/> containing the model's response.</returns>
    Task<ChatResult> ProcessAsync(IEnumerable<Message> messages, bool translate = false, Func<LLMTokenValue, Task>? tokenCallback = null, Func<ToolInvocation, Task>? toolCallback = null);
}