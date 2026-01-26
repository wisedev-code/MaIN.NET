using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.Knowledge;

namespace MaIN.Core.Hub.Contexts.Interfaces.AgentContext;

public interface IAgentActions
{
    /// <summary>
    /// Retrieves the unique identifier for the agent.
    /// </summary>
    /// <returns>A string representing the agent's unique identifier.</returns>
    string GetAgentId();

    /// <summary>
    /// Fetches the current agent instance.
    /// </summary>
    /// <returns>The <see cref="Agent"/> object containing all the properties and data for the current agent.</returns>
    Agent GetAgent();

    /// <summary>
    /// Gets the knowledge base (Knowledge) assigned to this Agent.
    /// </summary>
    /// <returns>A <see cref="Knowledge"/> object or null if not configured.</returns>
    Knowledge? GetKnowledge();

    /// <summary>
    /// Retrieves the chat session associated with the current agent.
    /// </summary>
    /// <returns>A <see cref="Chat"/> object representing the chat session associated with the agent.</returns>
    Task<Chat> GetChat();

    /// <summary>
    /// Restarts the chat session associated with the current agent, typically resetting the conversation state.
    /// </summary>
    /// <returns>A <see cref="Chat"/> object representing the restarted chat session.</returns>
    Task<Chat> RestartChat();

    /// <summary>
    /// Fetches all agents managed by the underlying agent service.
    /// </summary>
    /// <returns>A list of <see cref="Agent"/> objects representing all agents.</returns>
    Task<List<Agent>> GetAllAgents();

    /// <summary>
    /// Retrieves a specific agent by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the Agent.</param>
    /// <returns>The <see cref="Agent"/> object or null if not found.</returns>
    Task<Agent?> GetAgentById(string id);

    /// <summary>
    /// Deletes the current agent from the system.
    /// </summary>
    Task Delete();

    /// <summary>
    /// Checks if the current agent exists in the system.
    /// </summary>
    /// <returns>A boolean indicating whether the agent exists.</returns>
    Task<bool> Exists();
}