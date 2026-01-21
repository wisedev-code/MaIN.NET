namespace MaIN.Core.Hub.Contexts.Interfaces.AgentContext;

public interface IAgentBuilderEntryPoint : IAgentActions
{
    /// <summary>
    /// Sets the AI model for the agent to use during its interactions.
    /// </summary>
    /// <param name="model">The name or identifier of the AI model to be used.</param>
    /// <returns>The context instance implementing <see cref="IAgentConfigurationBuilder"/> for method chaining.</returns>
    IAgentConfigurationBuilder WithModel(string model);
    
    /// <summary>
    /// Specifies a custom model along with its file path for use by the agent. This allows using locally stored models in addition to predefined ones.
    /// </summary>
    /// <param name="model">The name of the custom model.</param>
    /// <param name="path">The path to the custom model’s file.</param>
    /// <param name="mmProject">Optional multi-modal project identifier.</param>
    /// <returns>The context instance implementing <see cref="IAgentConfigurationBuilder"/> for method chaining.</returns>
    IAgentConfigurationBuilder WithCustomModel(string model, string path, string? mmProject = null);
    
    /// <summary>
    /// Fetches an existing agent by its ID, allowing you to create a new <see cref="AgentContext"/> from an already existing agent.
    /// </summary>
    /// <param name="agentId">The unique identifier of the Agent to load.</param>
    /// <returns>The context instance implementing <see cref="IAgentContextExecutor"/> for method chaining.</returns>
    Task<IAgentContextExecutor> FromExisting(string agentId);
}