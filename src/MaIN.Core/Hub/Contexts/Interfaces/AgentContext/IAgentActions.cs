using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.Knowledge;

namespace MaIN.Core.Hub.Contexts.Interfaces.AgentContext;

public interface IAgentActions
{
    /// <summary>
    /// Gets the unique identifier (GUID) of the current Agent.
    /// </summary>
    /// <returns>The Agent's ID as a string.</returns>
    string GetAgentId();

    /// <summary>
    /// Retrieves the full Agent entity with its current configuration and state.
    /// </summary>
    /// <returns>An <see cref="Agent"/> object.</returns>
    Agent GetAgent();

    /// <summary>
    /// Gets the knowledge base (Knowledge) assigned to this Agent.
    /// </summary>
    /// <returns>A <see cref="Knowledge"/> object or null if not configured.</returns>
    Knowledge? GetKnowledge();

    /// <summary>
    /// Retrieves the current chat session associated with this Agent.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, containing the <see cref="Chat"/>.</returns>
    Task<Chat> GetChat();

    /// <summary>
    /// Clears the conversation history and restarts the chat session for this Agent.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, containing a new <see cref="Chat"/>.</returns>
    Task<Chat> RestartChat();

    /// <summary>
    /// Lists all agents available in the system.
    /// </summary>
    /// <returns>A task containing a list of <see cref="Agent"/> objects.</returns>
    Task<List<Agent>> GetAllAgents();

    /// <summary>
    /// Retrieves a specific agent by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the Agent.</param>
    /// <returns>The <see cref="Agent"/> object or null if not found.</returns>
    Task<Agent?> GetAgentById(string id);

    /// <summary>
    /// Permanently deletes the current Agent and all associated data.
    /// </summary>
    Task Delete();

    /// <summary>
    /// Checks if an agent with the current identifier exists in the system.
    /// </summary>
    /// <returns>True if the agent exists, otherwise false.</returns>
    Task<bool> Exists();
}