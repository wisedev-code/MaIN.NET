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
using MaIN.Services.Constants;

namespace MaIN.Core.Hub.Contexts;

public class AgentContext
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

    public AgentContext WithId(string id)
    {
        _agent.Id = id;
        return this;
    }
    
    public string GetAgentId() => _agent.Id;
    
    public Agent GetAgent() => _agent;
    
    public Knowledge? GetKnowledge() => _knowledge;
    
    public AgentContext WithOrder(int order)
    {
        _agent.Order = order;
        return this;
    }

    public AgentContext DisableCache()
    {
        _disableCache = true;
        return this;
    }

    public AgentContext WithSource(IAgentSource source, AgentSourceType type)
    {
        _agent.Context.Source = new AgentSource()
        {
            Details = source,
            Type = type
        };
        return this;
    }
    
    public AgentContext WithName(string name)
    {
        _agent.Name = name;
        return this;
    }

    public AgentContext WithBackend(BackendType backendType)
    {
        _agent.Backend = backendType;
        return this;
    }

    public AgentContext WithModel(string model)
    {
        _agent.Model = model;
        return this;
    }

    public AgentContext WithMcpConfig(Mcp mcpConfig)
    {
        if (_agent.Backend != null)
        {
            mcpConfig.Backend = _agent.Backend;
        }
        _agent.Context.McpConfig = mcpConfig;
        return this;
    }
    
    public AgentContext WithInferenceParams(InferenceParams inferenceParams)
    {
        _inferenceParams = inferenceParams;
        return this;
    }

    public AgentContext WithMemoryParams(MemoryParams memoryParams)
    {
        _memoryParams = memoryParams;
        return this;
    }

    public AgentContext WithCustomModel(string model, string path, string? mmProject = null)
    {
        KnownModels.AddModel(model, path, mmProject);
        _agent.Model = model;
        return this;
    }

    public AgentContext WithInitialPrompt(string prompt)
    {
        _agent.Context.Instruction = prompt;
        return this;
    }

    public AgentContext WithSteps(List<string>? steps)
    {
        _agent.Context.Steps = steps;
        return this;
    }

    public AgentContext WithKnowledge(Func<KnowledgeBuilder, KnowledgeBuilder> knowledgeConfig)
    {
        var builder = KnowledgeBuilder.Instance.ForAgent(_agent);
        _knowledge = knowledgeConfig(builder).Build();
        return this;
    }

    public AgentContext WithKnowledge(KnowledgeBuilder knowledge)
    {
        _knowledge = knowledge.ForAgent(_agent).Build();
        return this;
    }
    
    public AgentContext WithKnowledge(Knowledge knowledge)
    {
        _knowledge = knowledge;
        return this;
    }

    public AgentContext WithInMemoryKnowledge(Func<KnowledgeBuilder, KnowledgeBuilder> knowledgeConfig)
    {
        var builder = KnowledgeBuilder.Instance
            .ForAgent(_agent)
            .DisablePersistence();
        _knowledge = knowledgeConfig(builder).Build();
        return this;
    }
    
    public AgentContext WithBehaviour(string name, string instruction)
    {
        _agent.Behaviours ??= new Dictionary<string, string>();
        _agent.Behaviours[name] = instruction;
        _agent.CurrentBehaviour = name;
        return this;
    }

    public async Task<AgentContext> CreateAsync(bool flow = false, bool interactiveResponse = false)
    {
        await _agentService.CreateAgent(_agent, flow, interactiveResponse, _inferenceParams, _memoryParams, _disableCache);
        return this;
    }
    
    public AgentContext Create(bool flow = false, bool interactiveResponse = false)
    {
        _ = _agentService.CreateAgent(_agent, flow, interactiveResponse, _inferenceParams, _memoryParams, _disableCache).Result;
        return this;
    }

    public AgentContext WithTools(ToolsConfiguration toolsConfiguration)
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
    
    public async Task<ChatResult> ProcessAsync(string message, bool translate = false, Func<LLMTokenValue, Task>? callback = null)
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
        var result = await _agentService.Process(chat, _agent.Id, _knowledge, translate, callback);
        var messageResult = result.Messages.LastOrDefault()!;
        return new ChatResult()
        {
            Done = true,
            Model = result.Model,
            Message = messageResult,
            CreatedAt = DateTime.Now
        };
    }
    
    public async Task<ChatResult> ProcessAsync(Message message, bool translate = false, Func<LLMTokenValue, Task>? callback = null)
    {
        if (_knowledge == null)
        {
            LoadExistingKnowledgeIfExists();
        }
        var chat = await _agentService.GetChatByAgent(_agent.Id);
        chat.Messages.Add(message);
        var result = await _agentService.Process(chat, _agent.Id, _knowledge, translate, callback);
        var messageResult = result.Messages.LastOrDefault()!;
        return new ChatResult()
        {
            Done = true,
            Model = result.Model,
            Message = messageResult,
            CreatedAt = DateTime.Now
        };
    }
    
    public async Task<ChatResult> ProcessAsync(IEnumerable<Message> messages, bool translate = false, Func<LLMTokenValue, Task>? callback = null)
    {
        if (_knowledge == null)
        {
            LoadExistingKnowledgeIfExists();
        }
        var chat = await _agentService.GetChatByAgent(_agent.Id);
        var systemMsg = chat.Messages.FirstOrDefault(m => m.Role == ServiceConstants.Roles.System);
        chat.Messages.Clear();
        if (systemMsg != null)
        {
            chat.Messages.Add(systemMsg);
        }
        chat.Messages.AddRange(messages);
        var result = await _agentService.Process(chat, _agent.Id, _knowledge, translate, callback);
        var messageResult = result.Messages.LastOrDefault()!;
        return new ChatResult()
        {
            Done = true,
            Model = result.Model,
            Message = messageResult,
            CreatedAt = DateTime.Now
        };
    }

    public async Task<Chat> GetChat()
    {
        return await _agentService.GetChatByAgent(_agent.Id);
    }

    public async Task<Chat> RestartChat()
    {
        return await _agentService.Restart(_agent.Id);
    }

    public async Task<List<Agent>> GetAllAgents()
    {
        return await _agentService.GetAgents();
    }
    
    public async Task<Agent?> GetAgentById(string id)
    {
        return await _agentService.GetAgentById(id);
    }
    
    public async Task Delete()
    {
        await _agentService.DeleteAgent(_agent.Id);
    }

    public async Task<bool> Exists()
    {
        return await _agentService.AgentExists(_agent.Id);
    }

    public static async Task<AgentContext> FromExisting(IAgentService agentService, string agentId)
    {
        var existingAgent = await agentService.GetAgentById(agentId);
        if (existingAgent == null)
            throw new ArgumentException("Agent not found", nameof(agentId));
            
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