using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Domain.Exceptions.Agents;
using MaIN.Infrastructure.Models;
using System.Text.Json;

namespace MaIN.Infrastructure.Mappers;

internal static class AgentDocumentMapper
{
    internal static AgentDocument ToDocument(this Agent agent) => new()
    {
        Id = agent.Id,
        Name = agent.Name,
        Model = agent.Model,
        Order = agent.Order,
        Started = agent.Started,
        Flow = agent.Flow,
        ToolsConfiguration = agent.ToolsConfiguration,
        Backend = agent.Backend,
        ChatId = agent.ChatId,
        Description = agent.Description,
        Behaviours = agent.Behaviours,
        CurrentBehaviour = agent.CurrentBehaviour,
        Config = agent.Config.ToDocument()
    };

    internal static Agent ToDomain(this AgentDocument agent) => new()
    {
        Id = agent.Id,
        Name = agent.Name,
        Model = agent.Model,
        Started = agent.Started,
        Order = agent.Order,
        Flow = agent.Flow,
        ToolsConfiguration = agent.ToolsConfiguration,
        Backend = agent.Backend,
        ChatId = agent.ChatId,
        Description = agent.Description,
        Behaviours = agent.Behaviours,
        CurrentBehaviour = agent.CurrentBehaviour,
        Config = agent.Config?.ToDomain() ?? throw new AgentConfigNotFoundException(agent.Id)
    };

    internal static AgentConfigDocument ToDocument(this AgentConfig config) => new()
    {
        Instruction = config.Instruction,
        Relations = config.Relations?.ToList(),
        Steps = config.Steps?.ToList(),
        McpConfig = config.McpConfig,
        Source = config.Source is not null
            ? new AgentSourceDocument
            {
                DetailsSerialized = JsonSerializer.Serialize(config.Source.Details),
                AdditionalMessage = config.Source.AdditionalMessage,
                Type = Enum.Parse<AgentSourceTypeDocument>(config.Source.Type.ToString())
            }
            : null
    };

    internal static AgentConfig ToDomain(this AgentConfigDocument agentConfigDocument) => new()
    {
        Instruction = agentConfigDocument.Instruction,
        Relations = agentConfigDocument.Relations,
        McpConfig = agentConfigDocument.McpConfig,
        Source = agentConfigDocument.Source is not null
            ? new AgentSource
            {
                AdditionalMessage = agentConfigDocument.Source.AdditionalMessage,
                Details = agentConfigDocument.Source.DetailsSerialized,
                Type = Enum.Parse<AgentSourceType>(agentConfigDocument.Source.Type.ToString())
            }
            : null,
        Steps = agentConfigDocument.Steps
    };

    internal static AgentFlowDocument ToDocument(this AgentFlow agentFlow) => new()
    {
        Id = agentFlow.Id!,
        Name = agentFlow.Name,
        Description = agentFlow.Description!,
        Agents = [.. agentFlow.Agents.Select(x => x.ToDocument())]
    };

    internal static AgentFlow ToDomain(this AgentFlowDocument agentFlow) => new()
    {
        Id = agentFlow.Id,
        Name = agentFlow.Name,
        Description = agentFlow.Description,
        Agents = [.. agentFlow.Agents.Select(x => x.ToDomain())]
    };
}
