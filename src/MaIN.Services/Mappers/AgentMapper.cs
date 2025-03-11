using System.Text.Json;
using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Infrastructure.Models;
using MaIN.Models.Rag;
using MaIN.Services.Models.Rag;
using MaIN.Services.Models.Rag.AgentSource;

namespace MaIN.Services.Mappers;

public static class AgentMapper
{
    public static AgentDto ToDto(this Agent? agent)
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
            Context = agent.Context.ToDto()
        };

    public static AgentContextDto ToDto(this AgentData? agentContext)
        => new()
        {
            Instruction = agentContext.Instruction,
            Relations = agentContext.Relations,
            Steps = agentContext?.Steps ?? [],
            Source = agentContext?.Source is not null ? new AgentSourceDto()
            {
                Details = agentContext?.Source?.Details,
                AdditionalMessage = agentContext?.Source?.AdditionalMessage,
                Type = Enum.Parse<AgentSourceTypeDto>(agentContext.Source.Type.ToString())
            } : null
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
            Context = agent.Context.ToDomain()
        };

    public static AgentData ToDomain(this AgentContextDto agentContextDto)
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

    private static AgentSourceDetailsBase MapDetailsToType(object? details, AgentSourceTypeDto? sourceDetailsType)
    {
        return sourceDetailsType switch
        {
            AgentSourceTypeDto.Text => (AgentTextSourceDetails)details!,
            AgentSourceTypeDto.File => (AgentFileSourceDetails)details!,
            AgentSourceTypeDto.API => (AgentApiSourceDetails)details!,
            AgentSourceTypeDto.Web => (AgentWebSourceDetails)details!,
            //TBD add all types
            _ => new()
        };
    }

    public static AgentContextDocument ToDocument(this AgentData? context)
        => new()
        {
            Instruction = context.Instruction,
            Relations = context.Relations?.ToList(),
            Steps = context.Steps.ToList(),
            Source = context.Source is not null ? new AgentSourceDocument()
            {
                DetailsSerialized = JsonSerializer.Serialize(context.Source.Details),
                AdditionalMessage = context.Source.AdditionalMessage,
                Type = Enum.Parse<AgentSourceTypeDocument>(context.Source.Type.ToString())
            } : null
        };

    public static AgentDocument ToDocument(this Agent agent)
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
            Context = agent.Context.ToDocument()
        };
    
    public static Agent ToDomain(this AgentDocument? agent)
        => new()
        {
            Id = agent.Id,
            Name = agent.Name,
            Model = agent.Model,
            Started = agent.Started,
            Order = agent.Order,
            Flow = agent.Flow,
            Description = agent.Description,
            Behaviours = agent.Behaviours,
            CurrentBehaviour = agent.CurrentBehaviour,
            Context = agent.Context.ToDomain()
        };

    public static AgentData ToDomain(this AgentContextDocument agentContextDocument)
        => new()
        {
            Instruction = agentContextDocument.Instruction,
            Relations = agentContextDocument?.Relations,
            Source = new AgentSource
            {
                AdditionalMessage = agentContextDocument?.Source?.AdditionalMessage,
                Details = agentContextDocument?.Source?.DetailsSerialized,
                Type = Enum.Parse<AgentSourceType>(agentContextDocument?.Source?.Type.ToString() ?? AgentSourceType.Text.ToString())
            },
            Steps = agentContextDocument!.Steps
        };
}