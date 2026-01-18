namespace MaIN.Core.Hub.Contexts.Interfaces.AgentContext;

public interface IAgentBuilderEntryPoint : IAgentActions
{
    /// <summary>
    /// Sets the LLM model to be used by the Agent.
    /// </summary>
    /// <param name="model">The name or identifier of the model (e.g., "llama3.2").</param>
    IAgentConfigurationBuilder WithModel(string model);
    
    /// <summary>
    /// Configures a custom model from a specific local path.
    /// </summary>
    /// <param name="model">A custom name for the model.</param>
    /// <param name="path">The file system path to the model files.</param>
    /// <param name="mmProject">Optional multi-modal project identifier.</param>
    IAgentConfigurationBuilder WithCustomModel(string model, string path, string? mmProject = null);
    
    /// <summary>
    /// Loads an existing Agent from the database.
    /// </summary>
    /// <param name="agentId">The unique identifier of the Agent to load.</param>
    /// <returns>An execution interface ready for processing messages.</returns>
    Task<IAgentContextExecutor> FromExisting(string agentId);
}