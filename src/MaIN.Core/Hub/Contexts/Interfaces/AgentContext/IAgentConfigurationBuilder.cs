using MaIN.Core.Hub.Utils;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Domain.Entities.Agents.Knowledge;
using MaIN.Domain.Entities.Tools;

namespace MaIN.Core.Hub.Contexts.Interfaces.AgentContext;

public interface IAgentConfigurationBuilder : IAgentActions
{
    /// <summary>
    /// Sets the system-level instruction or persona for the Agent. This prompt defines how the Agent should behave and respond.
    /// </summary>
    /// <param name="prompt">The text content of the system instructions or Agent persona.</param>
    IAgentConfigurationBuilder WithInitialPrompt(string prompt);
    
    /// <summary>
    /// Sets a custom unique identifier for the Agent.
    /// </summary>
    /// <param name="id">The new Agent ID.</param>
    IAgentConfigurationBuilder WithId(string id);

    /// <summary>
    /// Sets the Agent's execution order (relevant in multi-agent flows).
    /// </summary>
    /// <param name="order">The sequence number.</param>
    IAgentConfigurationBuilder WithOrder(int order);

    /// <summary>
    /// Disables the caching mechanism for this Agent's requests.
    /// </summary>
    IAgentConfigurationBuilder DisableCache();

    /// <summary>
    /// Defines the data source from which the Agent derives its identity or knowledge.
    /// </summary>
    /// <param name="source">The implementation of the agent source.</param>
    /// <param name="type">The type of source (e.g., PDF, Web).</param>
    IAgentConfigurationBuilder WithSource(IAgentSource source, AgentSourceType type);

    /// <summary>
    /// Sets a friendly display name for the Agent.
    /// </summary>
    /// <param name="name">The name of the Agent.</param>
    IAgentConfigurationBuilder WithName(string name);

    /// <summary>
    /// Selects a specific processing engine (Backend) for LLM requests.
    /// </summary>
    /// <param name="backendType">The backend type.</param>
    IAgentConfigurationBuilder WithBackend(BackendType backendType);

    /// <summary>
    /// Configures integration with the Model Context Protocol (MCP).
    /// </summary>
    /// <param name="mcpConfig">The MCP configuration object.</param>
    IAgentConfigurationBuilder WithMcpConfig(Mcp mcpConfig);

    /// <summary>
    /// Sets inference parameters such as temperature, top-p, or max tokens.
    /// </summary>
    /// <param name="inferenceParams">An object containing LLM technical settings.</param>
    IAgentConfigurationBuilder WithInferenceParams(InferenceParams inferenceParams);

    /// <summary>
    /// Configures memory and context window settings for the Agent.
    /// </summary>
    /// <param name="memoryParams">Memory management settings.</param>
    IAgentConfigurationBuilder WithMemoryParams(MemoryParams memoryParams);

    /// <summary>
    /// Defines a sequence of steps (pipeline) that the Agent must execute for each request.
    /// </summary>
    /// <param name="steps">A list of step names.</param>
    IAgentConfigurationBuilder WithSteps(List<string>? steps);

    /// <summary>
    /// Assigns tools (functions) that the Agent can autonomously invoke.
    /// </summary>
    /// <param name="toolsConfiguration">The configuration for available tools.</param>
    IAgentConfigurationBuilder WithTools(ToolsConfiguration toolsConfiguration);

    /// <summary>
    /// Configures the Agent's knowledge base using a configuration delegate.
    /// </summary>
    /// <param name="knowledgeConfig">A function to configure the knowledge builder.</param>
    IAgentConfigurationBuilder WithKnowledge(Func<KnowledgeBuilder, KnowledgeBuilder> knowledgeConfig);

    /// <summary>
    /// Initializes the knowledge base using a pre-configured builder.
    /// </summary>
    /// <param name="knowledge">The knowledge builder instance.</param>
    IAgentConfigurationBuilder WithKnowledge(KnowledgeBuilder knowledge);

    /// <summary>
    /// Assigns a pre-built knowledge base object to the Agent.
    /// </summary>
    /// <param name="knowledge">The knowledge instance.</param>
    IAgentConfigurationBuilder WithKnowledge(Knowledge knowledge);

    /// <summary>
    /// Creates a volatile knowledge base stored only in memory (not persisted to disk).
    /// </summary>
    /// <param name="knowledgeConfig">A function to configure the in-memory knowledge builder.</param>
    IAgentConfigurationBuilder WithInMemoryKnowledge(Func<KnowledgeBuilder, KnowledgeBuilder> knowledgeConfig);

    /// <summary>
    /// Defines a specific personality or task (Behavior) under a unique name.
    /// </summary>
    /// <param name="name">The name of the behavior.</param>
    /// <param name="instruction">The system instructions for this behavior.</param>
    IAgentConfigurationBuilder WithBehaviour(string name, string instruction);

    /// <summary>
    /// Finalizes the build process and creates the Agent in the system synchronously.
    /// </summary>
    /// <param name="flow">Indicates if the Agent is part of a larger flow.</param>
    /// <param name="interactiveResponse">Specifies if responses should be streamed.</param>
    IAgentContextExecutor Create(bool flow = false, bool interactiveResponse = false);

    /// <summary>
    /// Finalizes the build process and creates the Agent in the system asynchronously.
    /// </summary>
    /// <param name="flow">Indicates if the Agent is part of a larger flow.</param>
    /// <param name="interactiveResponse">Specifies if responses should be interactive.</param>
    Task<IAgentContextExecutor> CreateAsync(bool flow = false, bool interactiveResponse = false);
}