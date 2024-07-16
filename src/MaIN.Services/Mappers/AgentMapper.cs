using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Infrastructure.Models;
using MaIN.Models.Rag;
using MaIN.Services.Steps;

namespace MaIN.Services.Mappers;

public static class AgentMapper
{
    public static AgentDto ToDto(this Agent agent)
        => new()
        {
            Id = agent.Id,
            Name = agent.Name,
            Model = agent.Model,
            Started = agent.Started,
            Description = agent.Description,
            Context = agent.Context.ToDto()
        };

    public static AgentContextDto ToDto(this AgentContext agentContext)
        => new()
        {
            Instruction = agentContext.Instruction,
            Relations = agentContext.Relations.Select(x => new AgentRelationDto()
            {
                Id = x.Id,
                AgentPurpose = x.AgentPurpose
            }).ToList(),
            Steps = new List<string>(),
            Source = new AgentSourceDto()
            {
                Details = agentContext.Source.Details,
                Type = Enum.Parse<AgentSourceTypeDto>(agentContext.Source.Type.ToString())
            }
        };

    public static Agent ToDomain(this AgentDto agent)
        => new()
        {
            Id = agent.Id,
            Name = agent.Name,
            Model = agent.Model,
            Started = agent.Started,
            Description = agent.Description,
            Context = agent.Context.ToDomain()
        };

    public static AgentContext ToDomain(this AgentContextDto agentContextDto)
        => new()
        {
            Instruction = agentContextDto.Instruction,
            Relations = agentContextDto?.Relations?.Select(x => new AgentRelation()
            {
                Id = x.Id,
                AgentPurpose = x.AgentPurpose
            }).ToList(),
            Source = new AgentSource()
            {
                Details = agentContextDto?.Source?.Details,
                Type = Enum.Parse<AgentSourceType>(agentContextDto?.Source?.Type.ToString()!)
            },
            Steps = agentContextDto!.Steps.ToLookup(x => x, y => Actions.Steps[y])
        };

    private static AgentSourceDetailsBase MapDetailsToType(object? details, AgentSourceTypeDto? sourceDetailsType)
    {
        return sourceDetailsType switch
        {
            AgentSourceTypeDto.Text => (AgentTextSourceDetails)details!,
            AgentSourceTypeDto.File => (AgentFileSourceDetails)details!,
            AgentSourceTypeDto.API => (AgentApiSourceDetails)details!,
            //TBD add all types
            _ => new()
        };
    }

    public static AgentContextDocument ToDocument(this AgentContext context)
        => new()
        {
            Instruction = context.Instruction,
            Relations = context.Relations?.Select(x => x.Id).ToList(),
            Steps = context.Steps.Select(x => x.Key).ToList(),
            Source = new AgentSourceDocument()
            {
                Details = context.Source.Details,
                Type = Enum.Parse<AgentSourceTypeDocument>(context.Source.Type.ToString())
            }
        };

    public static AgentDocument ToDocument(this Agent agent)
        => new()
        {
            Id = agent.Id,
            Name = agent.Name,
            Model = agent.Model,
            Started = agent.Started,
            Description = agent.Description,
            Context = agent.Context.ToDocument()
        };
}