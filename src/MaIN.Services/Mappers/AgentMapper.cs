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
            Config = agent.Config.ToDto()
        };

    public static AgentConfigDto ToDto(this AgentConfig agentConfig)
        => new()
        {
            Instruction = agentConfig.Instruction!,
            Relations = agentConfig.Relations,
            Steps = agentConfig.Steps ?? [],
            Source = (agentConfig.Source is not null ? new AgentSourceDto()
            {
                Details = agentConfig.Source?.Details,
                AdditionalMessage = agentConfig?.Source?.AdditionalMessage,
                Type = Enum.Parse<AgentSourceTypeDto>(agentConfig?.Source?.Type.ToString()!)
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
            Config = agent.Config.ToDomain()
        };

    public static AgentConfig ToDomain(this AgentConfigDto agentConfigDto)
        => new()
        {
            Instruction = agentConfigDto.Instruction,
            Relations = agentConfigDto?.Relations,
            Source = agentConfigDto?.Source is not null ? new AgentSource()
            {
                Details = agentConfigDto?.Source?.Details,
                AdditionalMessage = agentConfigDto?.Source?.AdditionalMessage,
                Type = Enum.Parse<AgentSourceType>(agentConfigDto?.Source?.Type.ToString()!)
            } : null,
            Steps = agentConfigDto!.Steps
        };
}
