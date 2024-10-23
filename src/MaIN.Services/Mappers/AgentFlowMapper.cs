using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Infrastructure.Models;
using MaIN.Models.Rag;

namespace MaIN.Services.Mappers;

public static class AgentFlowMapper
{
    public static AgentFlowDto ToDto(this AgentFlow agentFlow)
    {
        return new AgentFlowDto
        {
            Id = agentFlow.Id,
            Name = agentFlow.Name,
            Description = agentFlow.Description,
            Agents = agentFlow.Agents.OrderBy(x => x.Order).Select(x => x.ToDto()).ToList()
        };
    }

    public static AgentFlow ToDomain(this AgentFlowDto agentFlow) =>
        new()
        {
            Id = agentFlow.Id,
            Name = agentFlow.Name,
            Description = agentFlow.Description,
            Agents = agentFlow.Agents.Select(x => x.ToDomain()).ToList()
        };

    public static AgentFlow ToDomain(this AgentFlowDocument agentFlow) =>
        new()
        {
            Id = agentFlow.Id,
            Name = agentFlow.Name,
            Description = agentFlow.Description,
            Agents = agentFlow.Agents.Select(x => x.ToDomain()).ToList()
        };

    public static AgentFlowDocument ToDocument(this AgentFlow agentFlow) =>
        new()
        {
            Id = agentFlow.Id,
            Name = agentFlow.Name,
            Description = agentFlow.Description,
            Agents = agentFlow.Agents.Select(x => x.ToDocument()).ToList()
        };
    
    public static AgentFlow FromDocument(this AgentFlowDocument agentFlow) =>
        new()
        {
            Id = agentFlow.Id,
            Name = agentFlow.Name,
            Description = agentFlow.Description,
            Agents = agentFlow.Agents.Select(x => x.ToDomain()).ToList()
        };
}