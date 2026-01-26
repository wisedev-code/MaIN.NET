using MaIN.Core.Hub.Contexts.Interfaces.AgentContext;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Domain.Models;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;
using MaIN.Core.Hub.Utils;
using MaIN.Domain.Entities.Agents.Knowledge;
using MaIN.Domain.Entities.Tools;
using MaIN.Domain.Exceptions.Agents;
using MaIN.Services.Constants;

namespace MaIN.Core.Hub.Contexts;

public sealed class AgentContext : IAgentBuilderEntryPoint, IAgentConfigurationBuilder, IAgentContextExecutor
{
    private readonly IAgentService _agentService;
    private InferenceParams? _inferenceParams;
    private MemoryParams? _memoryParams;
    private bool _disableCache;
    private readonly Agent _agent;
    internal Knowledge? _knowledge;

    internal AgentContext(IAgentService agentService)
    {
        _agentService = agentService;
        _agent = new Agent
        {
            Id = Guid.NewGuid().ToString(),
            Behaviours = new Dictionary<string, string>(),
            Name = $"Agent-{Guid.NewGuid()}",
            Description = "Agent created by MaIN",
            CurrentBehaviour = "Default",
            Flow = false,
            Context = new AgentData()
            {
                Instruction = "Hello, I'm your personal assistant. How can I assist you today?",
                Relations = [],
                Source = null,
                Steps = ["ANSWER"]
            }
        };
    }

    internal AgentContext(IAgentService agentService, Agent existingAgent)
    {
        _agentService = agentService;
        _agent = existingAgent;
    }

    // --- IAgentActions ---
    public string GetAgentId() => _agent.Id;
    public Agent GetAgent() => _agent;
    public Knowledge? GetKnowledge() => _knowledge;
    public async Task<Chat> GetChat() => await _agentService.GetChatByAgent(_agent.Id);
    public async Task<Chat> RestartChat() => await _agentService.Restart(_agent.Id);
    public async Task<List<Agent>> GetAllAgents() => await _agentService.GetAgents();
    public async Task<Agent?> GetAgentById(string id) => await _agentService.GetAgentById(id);
    public async Task Delete() => await _agentService.DeleteAgent(_agent.Id);
    public async Task<bool> Exists() => await _agentService.AgentExists(_agent.Id);
    
    
    public IAgentConfigurationBuilder WithModel(string model)
    {
        _agent.Model = model;
        return this;
    }

    public IAgentConfigurationBuilder WithCustomModel(string model, string path, string? mmProject = null)
    {
        KnownModels.AddModel(model, path, mmProject);
        _agent.Model = model;
        return this;
    }

    public async Task<IAgentContextExecutor> FromExisting(string agentId)
    {
        var existingAgent = await _agentService.GetAgentById(agentId);
        if (existingAgent == null)
        {
            throw new AgentNotFoundException(agentId);
        }
            
        var context = new AgentContext(_agentService, existingAgent);
        context.LoadExistingKnowledgeIfExists();
        return context;
    }
    
    public IAgentConfigurationBuilder WithInitialPrompt(string prompt)
    {
        _agent.Context.Instruction = prompt;
        return this;
    }
    
    public IAgentConfigurationBuilder WithId(string id)
    {
        _agent.Id = id;
        return this;
    }

    public IAgentConfigurationBuilder WithOrder(int order)
    {
        _agent.Order = order;
        return this;
    }

    public IAgentConfigurationBuilder DisableCache()
    {
        _disableCache = true;
        return this;
    }

    public IAgentConfigurationBuilder WithSource(IAgentSource source, AgentSourceType type)
    {
        _agent.Context.Source = new AgentSource()
        {
            Details = source,
            Type = type
        };
        return this;
    }
    
    public IAgentConfigurationBuilder WithName(string name)
    {
        _agent.Name = name;
        return this;
    }

    public IAgentConfigurationBuilder WithBackend(BackendType backendType)
    {
        _agent.Backend = backendType;
        return this;
    }

    public IAgentConfigurationBuilder WithMcpConfig(Mcp mcpConfig)
    {
        if (_agent.Backend != null)
        {
            mcpConfig.Backend = _agent.Backend;
        }
        _agent.Context.McpConfig = mcpConfig;
        return this;
    }
    
    public IAgentConfigurationBuilder WithInferenceParams(InferenceParams inferenceParams)
    {
        _inferenceParams = inferenceParams;
        return this;
    }

    public IAgentConfigurationBuilder WithMemoryParams(MemoryParams memoryParams)
    {
        _memoryParams = memoryParams;
        return this;
    }

    public IAgentConfigurationBuilder WithSteps(List<string>? steps)
    {
        _agent.Context.Steps = steps;
        return this;
    }

    public IAgentConfigurationBuilder WithKnowledge(Func<KnowledgeBuilder, KnowledgeBuilder> knowledgeConfig)
    {
        var builder = KnowledgeBuilder.Instance.ForAgent(_agent);
        _knowledge = knowledgeConfig(builder).Build();
        return this;
    }

    public IAgentConfigurationBuilder WithKnowledge(KnowledgeBuilder knowledge)
    {
        _knowledge = knowledge.ForAgent(_agent).Build();
        return this;
    }
    
    public IAgentConfigurationBuilder WithKnowledge(Knowledge knowledge)
    {
        _knowledge = knowledge;
        return this;
    }

    public IAgentConfigurationBuilder WithInMemoryKnowledge(Func<KnowledgeBuilder, KnowledgeBuilder> knowledgeConfig)
    {
        var builder = KnowledgeBuilder.Instance
            .ForAgent(_agent)
            .DisablePersistence();
        _knowledge = knowledgeConfig(builder).Build();
        return this;
    }
    
    public IAgentConfigurationBuilder WithBehaviour(string name, string instruction)
    {
        _agent.Behaviours ??= new Dictionary<string, string>();
        _agent.Behaviours[name] = instruction;
        _agent.CurrentBehaviour = name;
        return this;
    }

    public async Task<IAgentContextExecutor> CreateAsync(bool flow = false, bool interactiveResponse = false)
    {
        await _agentService.CreateAgent(_agent, flow, interactiveResponse, _inferenceParams, _memoryParams, _disableCache);
        return this;
    }
    
    public IAgentContextExecutor Create(bool flow = false, bool interactiveResponse = false)
    {
        _ = _agentService.CreateAgent(_agent, flow, interactiveResponse, _inferenceParams, _memoryParams, _disableCache).Result;
        return this;
    }

    public IAgentConfigurationBuilder WithTools(ToolsConfiguration toolsConfiguration)
    {
        _agent.ToolsConfiguration = toolsConfiguration;
        return this;
    }
    
    internal void LoadExistingKnowledgeIfExists()
    {
        _knowledge ??= new Knowledge(_agent);

        try
        {
            _knowledge.Load();
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("Knowledge cannot be loaded - new one will be created");
        }
    }
    
    public async Task<ChatResult> ProcessAsync(Chat chat, bool translate = false)
    {
        if (_knowledge == null)
        {
            LoadExistingKnowledgeIfExists();
        }

        var result = await _agentService.Process(chat, _agent.Id, _knowledge, translate);
        var message = result.Messages.LastOrDefault()!;
        return new ChatResult()
        {
            Done = true,
            Model = result.Model,
            Message = message,
            CreatedAt = DateTime.Now
        };
    }
    
    public async Task<ChatResult> ProcessAsync(
        string message,
        bool translate = false,
        Func<LLMTokenValue, Task>? tokenCallback = null,
        Func<ToolInvocation, Task>? toolCallback = null)
    {
        if (_knowledge == null)
        {
            LoadExistingKnowledgeIfExists();
        }
        var chat = await _agentService.GetChatByAgent(_agent.Id);
        chat.Messages.Add(new Message()
        {
            Content = message,
            Role = "User",
            Type = MessageType.LocalLLM,
            Time = DateTime.Now
        });
        var result = await _agentService.Process(chat, _agent.Id, _knowledge, translate, tokenCallback, toolCallback);
        var messageResult = result.Messages.LastOrDefault()!;
        return new ChatResult()
        {
            Done = true,
            Model = result.Model,
            Message = messageResult,
            CreatedAt = DateTime.Now
        };
    }
    
    public async Task<ChatResult> ProcessAsync(Message message, 
        bool translate = false,
        Func<LLMTokenValue, Task>? tokenCallback = null,
        Func<ToolInvocation, Task>? toolCallback = null)
    {
        if (_knowledge == null)
        {
            LoadExistingKnowledgeIfExists();
        }
        var chat = await _agentService.GetChatByAgent(_agent.Id);
        chat.Messages.Add(message);
        var result = await _agentService.Process(chat, _agent.Id, _knowledge, translate, tokenCallback, toolCallback);;
        var messageResult = result.Messages.LastOrDefault()!;
        return new ChatResult()
        {
            Done = true,
            Model = result.Model,
            Message = messageResult,
            CreatedAt = DateTime.Now
        };
    }
    
    public async Task<ChatResult> ProcessAsync(
        IEnumerable<Message> messages,
        bool translate = false,
        Func<LLMTokenValue, Task>? tokenCallback = null,
        Func<ToolInvocation, Task>? toolCallback = null)
    {
        if (_knowledge == null)
        {
            LoadExistingKnowledgeIfExists();
        }
        var chat = await _agentService.GetChatByAgent(_agent.Id);
        var systemMsg = chat.Messages.FirstOrDefault(m => m.Role.Equals(ServiceConstants.Roles.System, StringComparison.InvariantCultureIgnoreCase));
        chat.Messages.Clear();
        if (systemMsg != null)
        {
            chat.Messages.Add(systemMsg);
        }
        chat.Messages.AddRange(messages);
        var result = await _agentService.Process(chat, _agent.Id, _knowledge, translate, tokenCallback, toolCallback);;
        var messageResult = result.Messages.LastOrDefault()!;
        return new ChatResult()
        {
            Done = true,
            Model = result.Model,
            Message = messageResult,
            CreatedAt = DateTime.Now
        };
    }

    public static async Task<AgentContext> FromExisting(IAgentService agentService, string agentId)
    {
        var existingAgent = await agentService.GetAgentById(agentId);
        if (existingAgent == null)
        {
            throw new AgentNotFoundException(agentId);
        }
            
        var context = new AgentContext(agentService, existingAgent);
        context.LoadExistingKnowledgeIfExists();
        return context;
    }
}

public static class AgentExtensions
{
    public static async Task<ChatResult> ProcessAsync(
        this Task<AgentContext> agentTask, 
        string message, 
        bool translate = false)
    {
        var agent = await agentTask;
        if (agent._knowledge == null)
        {
            agent.LoadExistingKnowledgeIfExists();
        }
        return await agent.ProcessAsync(message, translate);
    }
}