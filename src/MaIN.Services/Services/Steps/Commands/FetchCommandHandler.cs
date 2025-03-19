using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models.Commands;
using System.Text.Json;
using MaIN.Services.Mappers;

namespace MaIN.Services.Services.Steps.Commands;

public class FetchCommandHandler(
    IHttpClientFactory httpClientFactory,
    IDataSourceProvider dataSourceService,
    ILLMService llmService) : ICommandHandler<FetchCommand, Message?>
{
    public async Task<Message?> HandleAsync(FetchCommand command)
    {
        var properties = new Dictionary<string, string>
        {
            { "agent_internal", "true" }
        };

        switch (command.Context.Source!.Type)
        {
            case AgentSourceType.File:
                return await HandleFileSource(command, properties);
                
            case AgentSourceType.Web:
                return await HandleWebSource(command, properties);
                
            case AgentSourceType.Text:
                var textData = dataSourceService.FetchTextData(command.Context.Source.Details);
                return CreateMessage(textData, properties);
                
            case AgentSourceType.API:
                var apiData = await dataSourceService.FetchApiData(
                    command.Context.Source.Details,
                    command.Filter,
                    httpClientFactory,
                    properties);
                return CreateMessage(apiData, properties);
                
            case AgentSourceType.SQL:
                var sqlData = await dataSourceService.FetchSqlData(
                    command.Context.Source.Details,
                    command.Filter,
                    properties);
                return CreateMessage(sqlData, properties);
                
            case AgentSourceType.NoSQL:
                var noSqlData = await dataSourceService.FetchNoSqlData(
                    command.Context.Source.Details,
                    command.Filter,
                    properties);
                return CreateMessage(noSqlData, properties);
                
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task<Message> HandleFileSource(FetchCommand command, Dictionary<string, string> properties)
    {
        var fileData = JsonSerializer.Deserialize<AgentFileSourceDetails>(command.Context.Source!.Details?.ToString()!);
        
        if (command.Chat?.Messages.Count > 0)
        {
            var memoryChat = CreateMemoryChat(command);
            var result = await llmService.AskMemory(
                memoryChat, 
                fileData: new Dictionary<string, string>() { { fileData!.Name, fileData.Path } }
            );
            
            return result!.Message.ToDomain();
        }
        
        var data = await dataSourceService.FetchFileData(command.Context.Source.Details);
        return CreateMessage(data, properties);
    }

    private async Task<Message> HandleWebSource(FetchCommand command, Dictionary<string, string> properties)
    {
        var webData = JsonSerializer.Deserialize<AgentWebSourceDetails>(command.Context.Source!.Details?.ToString()!);
        
        if (command.Chat?.Messages.Count > 0)
        {
            var memoryChat = CreateMemoryChat(command);
            var result = await llmService.AskMemory(memoryChat, webUrls: [webData!.Url]);
            return result!.Message.ToDomain();
        }

        return CreateMessage($"Web data from {webData!.Url}", properties);
    }
    
    private static Chat CreateMemoryChat(FetchCommand command)
    {
        if (command.Chat == null)
        {
            throw new ArgumentNullException(nameof(command.Chat));
        }
        
        return new Chat
        {
            Messages = command.Chat.Messages,
            Model = command.Chat.Model,
            Properties = command.Chat.Properties,
            Name = "Memory Chat",
            Id = Guid.NewGuid().ToString()
        };
    }
    
    private static Message CreateMessage(string content, Dictionary<string, string> properties)
    {
        return new Message
        {
            Content = content,
            Role = "User",
            Properties = properties
        };
    }
}