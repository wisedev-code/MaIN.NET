using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Services.Services.Models;

namespace MaIN.Core.Hub.Contexts.Interfaces.FlowContext;

public interface IFlowContext
{
    /// <summary>
    /// Assigns a unique identifier to the flow.
    /// </summary>
    /// <param name="id">The unique identifier for the flow.</param>
    /// <returns>The IFlowContext instance to enable method chaining.</returns>
    IFlowContext WithId(string id);

    /// <summary>
    /// Assigns a custom name to the flow.
    /// </summary>
    /// <param name="name">The custom name for the flow.</param>
    /// <returns>The IFlowContext instance to enable method chaining.</returns>
    IFlowContext WithName(string name);

    /// <summary>
    /// Sets a description for the flow.
    /// </summary>
    /// <param name="description">A brief description of the flow's purpose.</param>
    /// <returns>The IFlowContext instance to enable method chaining.</returns>
    IFlowContext WithDescription(string description);

    /// <summary>
    /// Saves the current flow and its associated agents to a zip archive at the specified path.
    /// This method also includes a text file for the flow description.
    /// </summary>
    /// <param name="path">The file path where the flow and its agents should be saved.</param>
    /// <returns>The IFlowContext instance to enable method chaining.</returns>
    IFlowContext Save(string path);

    /// <summary>
    /// Loads an existing flow from a zip archive located at the specified path.
    /// This archive should contain the flow description and agent files in JSON format.
    /// </summary>
    /// <param name="path">The file path where the flow archive is stored.</param>
    /// <returns>The IFlowContext instance to enable method chaining.</returns>
    IFlowContext Load(string path);

    /// <summary>
    /// Adds an agent to the flow. This allows you to dynamically update the flow with new agents.
    /// </summary>
    /// <param name="agent">The Agent to be added to the flow.</param>
    /// <returns>The IFlowContext instance to enable method chaining.</returns>
    IFlowContext AddAgent(Agent agent);

    /// <summary>
    /// Adds a collection of agents to the flow. This method enables the batch addition of multiple agents at once.
    /// </summary>
    /// <param name="agents">The collection of agents to be added to the flow.</param>
    /// <returns>The IFlowContext instance to enable method chaining.</returns>
    IFlowContext AddAgents(IEnumerable<Agent> agents);

    /// <summary>
    /// Processes a chat through the first agent in the flow, generating a response based on the chat's messages and the agent's context.
    /// </summary>
    /// <param name="chat">The Chat object to process.</param>
    /// <param name="translate">A flag indicating whether the response should be translated. Default is false.</param>
    /// <returns>A ChatResult object containing the processed message and other related information.</returns>
    Task<ChatResult> ProcessAsync(Chat chat, bool translate = false);

    /// <summary>
    /// Processes a user-provided message through the first agent in the flow, generating a response based on the agent's context.
    /// </summary>
    /// <param name="message">The message to be processed by the agent.</param>
    /// <param name="translate">A flag indicating whether the response should be translated. Default is false.</param>
    /// <returns>A ChatResult object containing the processed message and other related information.</returns>
    Task<ChatResult> ProcessAsync(string message, bool translate = false);

    /// <summary>
    /// Processes a message object through the first agent in the flow, generating a response based on the agent's context and message data.
    /// </summary>
    /// <param name="message">The Message object to be processed.</param>
    /// <param name="translate">A flag indicating whether the response should be translated. Default is false.</param>
    /// <returns>A ChatResult object containing the processed message and other related information.</returns>
    Task<ChatResult> ProcessAsync(Message message, bool translate = false);

    /// <summary>
    /// Creates and persists the current flow asynchronously.
    /// </summary>
    /// <returns>The created AgentFlow object.</returns>
    Task<AgentFlow> CreateAsync();

    /// <summary>
    /// Deletes the current flow from the system.
    /// </summary>
    /// <returns>A task representing the asynchronous delete operation.</returns>
    /// <exception cref="MaIN.Domain.Exceptions.Flows.FlowNotInitializedException">Thrown if the flow ID is not set.</exception>
    Task Delete();

    /// <summary>
    /// Retrieves the current flow with its latest state from the system.
    /// </summary>
    /// <returns>The current AgentFlow object.</returns>
    /// <exception cref="MaIN.Domain.Exceptions.Flows.FlowNotInitializedException">Thrown if the flow ID is not set.</exception>
    Task<AgentFlow> GetCurrentFlow();

    /// <summary>
    /// Retrieves all flows available in the system.
    /// </summary>
    /// <returns>A list of all AgentFlow objects.</returns>
    Task<List<AgentFlow>> GetAllFlows();

    /// <summary>
    /// Loads an existing flow from the system by its ID and initializes the context with it.
    /// </summary>
    /// <param name="flowId">The unique identifier of the flow to load.</param>
    /// <returns>The IFlowContext instance initialized with the existing flow.</returns>
    /// <exception cref="MaIN.Domain.Exceptions.Flows.FlowFoundException">Thrown if the flow with the specified ID is not found.</exception>
    Task<IFlowContext> FromExisting(string flowId);
}