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
    /// Sets the initial prompt for the agent. This prompt serves as an instruction or context that guides the agent's behavior during its execution.
    /// </summary>
    /// <param name="prompt"> The initial prompt or instruction for the agent.</param>
    /// <returns>The context instance implementing <see cref="IAgentConfigurationBuilder"/> for method chaining.</returns>
    IAgentConfigurationBuilder WithInitialPrompt(string prompt);
    
    /// <summary>
    /// Assigns a unique identifier to the agent.
    /// </summary>
    /// <param name="id">he unique identifier for the agent.</param>
    /// <returns>The context instance implementing <see cref="IAgentConfigurationBuilder"/> for method chaining.</returns>
    IAgentConfigurationBuilder WithId(string id);

    /// <summary>
    /// Sets the order of the agent. This can be used in scenarios where agents need to be prioritized or sequenced.
    /// </summary>
    /// <param name="order">The order value to assign to the agent.</param>
    /// <returns>The context instance implementing <see cref="IAgentConfigurationBuilder"/> for method chaining.</returns>
    IAgentConfigurationBuilder WithOrder(int order);

    /// <summary>
    /// Each time we run inference, we need to load model into memory, this takes time and memory. This method allows us to save some
    /// more of GPU/RAM resources at the cost of time, because model weights are no longer cached.
    /// </summary>
    /// <returns>The context instance implementing <see cref="IAgentConfigurationBuilder"/> for method chaining.</returns>
    IAgentConfigurationBuilder DisableCache();

    /// <summary>
    /// Sets the source of the agent’s context, including information related to the agent's source and its type.
    /// </summary>
    /// <param name="source">The <see cref="IAgentSource"/> source instance providing the agent’s context.</param>
    /// <param name="type">The <see cref="AgentSourceType"/> - type of source (e.g., API, SQL, Web).</param>
    /// <returns>The context instance implementing <see cref="IAgentConfigurationBuilder"/> for method chaining.</returns>
    IAgentConfigurationBuilder WithSource(IAgentSource source, AgentSourceType type);

    /// <summary>
    /// Sets a friendly display name for the Agent.
    /// </summary>
    /// <param name="name">The name of the Agent.</param>
    /// <returns>The context instance implementing <see cref="IAgentConfigurationBuilder"/> for method chaining.</returns>
    IAgentConfigurationBuilder WithName(string name);

    /// <summary>
    /// Defines backend that will be used for model inference.
    /// </summary>
    /// <param name="backendType">The <see cref="BackendType"/> - an enum that defines which AI backend to use.</param>
    /// <returns>The context instance implementing <see cref="IAgentConfigurationBuilder"/> for method chaining.</returns>
    IAgentConfigurationBuilder WithBackend(BackendType backendType);

    /// <summary>
    /// Configures integration with the Model Context Protocol (MCP).
    /// </summary>
    /// <param name="mcpConfig">The <see cref="Mcp"/> configuration object.</param>
    /// <returns>The context instance implementing <see cref="IAgentConfigurationBuilder"/> for method chaining.</returns>
    IAgentConfigurationBuilder WithMcpConfig(Mcp mcpConfig);

    /// <summary>
    /// Sets the inference parameters for the chat session, allowing you to customize how the AI processes and generates responses
    /// based on specific parameters. Inference parameters can influence various aspects of the chat, such as response length,
    /// temperature, and other model-specific settings.
    /// </summary>
    /// <param name="inferenceParams">An <see cref="InferenceParams"/> object that holds the parameters for inference, such as Temperature, MaxTokens,
    /// TopP, etc. These parameters control the generation behavior of the agent.</param>
    /// <returns>The context instance implementing <see cref="IAgentConfigurationBuilder"/> for method chaining.</returns>
    IAgentConfigurationBuilder WithInferenceParams(InferenceParams inferenceParams);

    /// <summary>
    /// Sets the memory parameters for the chat session, allowing you to customize how the AI accesses and uses its memory
    /// for generating responses. Memory parameters influence aspects such as context size, memory search depth,
    /// and token allocation for responses.
    /// </summary>
    /// <param name="memoryParams">A <see cref="MemoryParams"/> object that holds the parameters for memory management, such as
    /// ContextSize, MaxMatchesCount, AnswerTokens, etc. These parameters control how agent uses memory for response generation.</param>
    /// <returns>The context instance implementing <see cref="IAgentConfigurationBuilder"/> for method chaining.</returns>
    IAgentConfigurationBuilder WithMemoryParams(MemoryParams memoryParams);

    /// <summary>
    /// Configures the steps that the agent will follow during its interaction. Each step is a task or action that
    /// the agent will execute sequentially.
    /// </summary>
    /// <param name="steps">A list of strings representing the steps the agent should follow.</param>
    /// <returns>The context instance implementing <see cref="IAgentConfigurationBuilder"/> for method chaining.</returns>
    IAgentConfigurationBuilder WithSteps(List<string>? steps);

    /// <summary>
    /// Assigns tools (functions) that the Agent can autonomously invoke.
    /// </summary>
    /// <param name="toolsConfiguration">The <see cref="ToolsConfiguration"/> for available tools.</param>
    /// <returns>The context instance implementing <see cref="IAgentConfigurationBuilder"/> for method chaining.</returns>
    IAgentConfigurationBuilder WithTools(ToolsConfiguration toolsConfiguration);

    /// <summary>
    /// Configures the Agent's knowledge base using a configuration delegate.
    /// </summary>
    /// <param name="knowledgeConfig">A function to configure the knowledge builder.</param>
    /// <returns>The context instance implementing <see cref="IAgentConfigurationBuilder"/> for method chaining.</returns>
    IAgentConfigurationBuilder WithKnowledge(Func<KnowledgeBuilder, KnowledgeBuilder> knowledgeConfig);

    /// <summary>
    /// Initializes the knowledge base using a pre-configured builder.
    /// </summary>
    /// <param name="knowledge">The <see cref="KnowledgeBuilder"/> instance.</param>
    /// <returns>The context instance implementing <see cref="IAgentConfigurationBuilder"/> for method chaining.</returns>
    IAgentConfigurationBuilder WithKnowledge(KnowledgeBuilder knowledge);

    /// <summary>
    /// Assigns a pre-built knowledge base object to the Agent.
    /// </summary>
    /// <param name="knowledge">The <see cref="Knowledge"/> instance.</param>
    /// <returns>The context instance implementing <see cref="IAgentConfigurationBuilder"/> for method chaining.</returns>
    IAgentConfigurationBuilder WithKnowledge(Knowledge knowledge);

    /// <summary>
    /// Creates a volatile knowledge base stored only in memory.
    /// </summary>
    /// <param name="knowledgeConfig">A function to configure the in-memory knowledge builder.</param>
    /// <returns>The context instance implementing <see cref="IAgentConfigurationBuilder"/> for method chaining.</returns>
    IAgentConfigurationBuilder WithInMemoryKnowledge(Func<KnowledgeBuilder, KnowledgeBuilder> knowledgeConfig);

    /// <summary>
    /// Defines a behavior for the agent, specifying an action or task the agent should perform based on the provided name and instruction.
    /// </summary>
    /// <param name="name">The name of the behavior.</param>
    /// <param name="instruction">The instruction associated with the behavior.</param>
    /// <returns>The context instance implementing <see cref="IAgentConfigurationBuilder"/> for method chaining.</returns>
    IAgentConfigurationBuilder WithBehaviour(string name, string instruction);

    /// <summary>
    /// Synchronously creates the agent in the system.
    /// </summary>
    /// <param name="flow">A flag indicating whether the agent should be part of an agent flow.</param>
    /// <param name="interactiveResponse">A flag indicating whether the agent should generate interactive responses.</param>
    /// <returns>The context instance implementing <see cref="IAgentContextExecutor"/> for method chaining.</returns>
    IAgentContextExecutor Create(bool flow = false, bool interactiveResponse = false);

    /// <summary>
    /// Asynchronously creates the agent in the system. This method integrates the agent into the underlying agent service,
    /// making it ready for use.
    /// </summary>
    /// <param name="flow">A flag indicating whether the agent should be part of an agent flow.</param>
    /// <param name="interactiveResponse">A flag indicating whether the agent should generate interactive responses.</param>
    /// <returns>The context instance implementing <see cref="IAgentContextExecutor"/> for method chaining.</returns>
    Task<IAgentContextExecutor> CreateAsync(bool flow = false, bool interactiveResponse = false);
}