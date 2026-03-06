using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Services.Dtos.Rag;
using MaIN.Services.Dtos.Rag.AgentSource;

namespace MaIN.Services.Mappers;

public static class AgentMapper
{
    public static AgentDto ToDto(this Agent agent)
        => new()
        {
            Id = agent.Id,
            Order = agent.Order,
            Name = agent.Name,
            Model = agent.Model,
            Started = agent.Started,
            Flow = agent.Flow,
            Description = agent.Description,
            Behaviours = agent.Behaviours,
            CurrentBehaviour = agent.CurrentBehaviour,
            Context = agent.Config.ToDto()
        };

    public static AgentConfigDto ToDto(this AgentConfig agentContext)
        => new()
        {
            Instruction = agentContext.Instruction!,
            Relations = agentContext.Relations,
            Steps = agentContext.Steps ?? [],
            Source = (agentContext.Source is not null ? new AgentSourceDto()
            {
                Details = agentContext.Source?.Details,
                AdditionalMessage = agentContext?.Source?.AdditionalMessage,
                Type = Enum.Parse<AgentSourceTypeDto>(agentContext?.Source?.Type.ToString()!)
            } : null)!
        };

    public static Agent ToDomain(this AgentDto agent)
        => new()
        {
            Id = agent.Id,
            Name = agent.Name,
            Model = agent.Model,
            Order = agent.Order,
            Started = agent.Started,
            Flow = agent.Flow,
            Description = agent.Description,
            Behaviours = agent.Behaviours,
            CurrentBehaviour = agent.CurrentBehaviour,
            Config = agent.Context.ToDomain()
        };

    public static AgentConfig ToDomain(this AgentConfigDto agentContextDto)
        => new()
        {
            Instruction = agentContextDto.Instruction,
            Relations = agentContextDto?.Relations,
            Source = agentContextDto?.Source is not null ? new AgentSource()
            {
                Details = agentContextDto?.Source?.Details,
                AdditionalMessage = agentContextDto?.Source?.AdditionalMessage,
                Type = Enum.Parse<AgentSourceType>(agentContextDto?.Source?.Type.ToString()!)
            } : null,
            Steps = agentContextDto!.Steps
        };
}
