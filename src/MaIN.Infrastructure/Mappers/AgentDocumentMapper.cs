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
        Description = agent.Description,
        Behaviours = agent.Behaviours,
        CurrentBehaviour = agent.CurrentBehaviour,
        Config = agent.Config?.ToDomain() ?? throw new AgentContextNotFoundException(agent.Id)
    };

    internal static AgentConfigDocument ToDocument(this AgentConfig context) => new()
    {
        Instruction = context.Instruction,
        Relations = context.Relations?.ToList(),
        Steps = context.Steps?.ToList(),
        McpConfig = context.McpConfig,
        Source = context.Source is not null
            ? new AgentSourceDocument
            {
                DetailsSerialized = JsonSerializer.Serialize(context.Source.Details),
                AdditionalMessage = context.Source.AdditionalMessage,
                Type = Enum.Parse<AgentSourceTypeDocument>(context.Source.Type.ToString())
            }
            : null
    };

    internal static AgentConfig ToDomain(this AgentConfigDocument agentContextDocument) => new()
    {
        Instruction = agentContextDocument.Instruction,
        Relations = agentContextDocument.Relations,
        McpConfig = agentContextDocument.McpConfig,
        Source = agentContextDocument.Source is not null
            ? new AgentSource
            {
                AdditionalMessage = agentContextDocument.Source.AdditionalMessage,
                Details = agentContextDocument.Source.DetailsSerialized,
                Type = Enum.Parse<AgentSourceType>(agentContextDocument.Source.Type.ToString())
            }
            : null,
        Steps = agentContextDocument.Steps
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
