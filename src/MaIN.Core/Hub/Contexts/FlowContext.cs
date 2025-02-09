using System.IO.Compression;
using System.Text.Json;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Services.Mappers;
using MaIN.Services.Models;
using MaIN.Services.Services.Abstract;

namespace MaIN.Core.Hub.Contexts;

public class FlowContext
{
    private readonly IAgentFlowService _flowService;
    private readonly IAgentService _agentService;
    private AgentFlow _flow;
    private Agent? _firstAgent => _flow.Agents.MinBy(c => c.Order);

    internal FlowContext(IAgentFlowService flowService, IAgentService agentService)
    {
        _flowService = flowService;
        _agentService = agentService;
        _flow = new AgentFlow
        {
            Id = Guid.NewGuid().ToString(),
            Agents = new List<Agent>(),
        };
    }

    internal FlowContext(IAgentFlowService flowService, IAgentService agentService, AgentFlow existingFlow)
    {
        _flowService = flowService;
        _agentService = agentService;
        _flow = existingFlow;
    }

    public FlowContext WithId(string id)
    {
        _flow.Id = id;
        return this;
    }

    public FlowContext WithName(string name)
    {
        _flow.Name = name;
        return this;
    }
    
    public FlowContext WithDescription(string description)
    {
        _flow.Name = description;
        return this;
    }
    
    public FlowContext Save(string path)
    {
// Ensure the directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        using (var fileStream = new FileStream(path, FileMode.Create))
        using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
        {
            var descriptionEntry = archive.CreateEntry("description.txt");
            using (var entryStream = descriptionEntry.Open())
            using (var writer = new StreamWriter(entryStream))
            {
                writer.Write(_flow.Description);
            }

            foreach (var agent in _flow.Agents)
            {
                var agentFileName = $"{agent.Id}.json";
                var agentEntry = archive.CreateEntry(agentFileName);
                using (var entryStream = agentEntry.Open())
                using (var writer = new StreamWriter(entryStream))
                {
                    var json = JsonSerializer.Serialize(agent);
                    writer.Write(json);
                }
            }
        }

        return this;
    }

    public FlowContext Load(string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        string description = "";
        var agents = new List<Agent>();

        using (var fileStream = new FileStream(path, FileMode.Open))
        using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
        {
            // Read description from description.txt
            var descriptionEntry = archive.GetEntry("description.txt");
            if (descriptionEntry != null)
            {
                using (var entryStream = descriptionEntry.Open())
                using (var reader = new StreamReader(entryStream))
                {
                    description = reader.ReadToEnd();
                }
            }

            // Read agents from JSON files
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    using (var entryStream = entry.Open())
                    using (var reader = new StreamReader(entryStream))
                    {
                        var json = reader.ReadToEnd();
                        var agent = JsonSerializer.Deserialize<Agent>(json);
                        if (agent != null)
                        {
                            agents.Add(agent);
                        }
                    }
                }
            }
        }

        // Reinitialize the flow with loaded data
        _flow = new AgentFlow
        {
            Id = Guid.NewGuid().ToString(), // Generate new Id for the loaded flow
            Name = fileName,
            Description = description,
            Agents = agents
        };

        return this;
        return this;
    }

    public FlowContext AddAgent(Agent agent)
    {
        _flow.Agents.Add(agent);
        return this;
    }
    
    
    public async Task<ChatResult> ProcessAsync(Chat chat, bool translate = false)
    {
        var result = await _agentService.Process(chat, _firstAgent!.Id, translate);
        var message = result!.Messages!.LastOrDefault()!.ToDto();
        return new ChatResult()
        {
            Done = true,
            Model = result!.Model,
            Message = message,
            CreatedAt = DateTime.Now
        };
    }
    
    public async Task<ChatResult> ProcessAsync(string message, bool translate = false)
    {
        var chat = await _agentService.GetChatByAgent(_firstAgent!.Id);
        chat.Messages?.Add(new Message()
        {
            Content = message,
            Role = "User",
            Time = DateTime.Now
        });
        var result = await _agentService.Process(chat, _firstAgent.Id, translate);
        var messageResult = result!.Messages!.LastOrDefault()!.ToDto();
        return new ChatResult()
        {
            Done = true,
            Model = result!.Model,
            Message = messageResult,
            CreatedAt = DateTime.Now
        };
    }
    
    public async Task<ChatResult> ProcessAsync(Message message, bool translate = false)
    {
        var chat = await _agentService.GetChatByAgent(_firstAgent!.Id);
        chat.Messages?.Add(message);
        var result = await _agentService.Process(chat, _firstAgent.Id, translate);
        var messageResult = result!.Messages!.LastOrDefault()!.ToDto();
        return new ChatResult()
        {
            Done = true,
            Model = result!.Model,
            Message = messageResult,
            CreatedAt = DateTime.Now
        };
    }
    
    public FlowContext AddAgents(IEnumerable<Agent> agents)
    {
        _flow.Agents.AddRange(agents);
        return this;
    }
    
    public async Task<AgentFlow> CreateAsync()
    {
        return await _flowService.CreateFlow(_flow);
    }

    public async Task Delete()
    {
        if (_flow.Id == null)
            throw new InvalidOperationException("Flow has not been created yet.");
            
        await _flowService.DeleteFlow(_flow.Id);
    }

    // Retrieval Methods
    public async Task<AgentFlow> GetCurrentFlow()
    {
        if (_flow.Id == null)
            throw new InvalidOperationException("Flow has not been created yet.");
            
        return await _flowService.GetFlowById(_flow.Id);
    }

    public async Task<List<AgentFlow>> GetAllFlows()
    {
        return await _flowService.GetAllFlows();
    }

    // Static factory methods
    public async Task<FlowContext> FromExisting(string flowId)
    {
        var existingFlow = await _flowService.GetFlowById(flowId);
        if (existingFlow == null)
            throw new ArgumentException("Flow not found", nameof(flowId));

        return this;
    }
}