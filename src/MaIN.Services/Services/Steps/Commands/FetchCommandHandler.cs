using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models.Commands;
using System.Text.Json;
using MaIN.Domain.Configuration;
using MaIN.Services.Services.LLMService;
using MaIN.Services.Services.LLMService.Factory;
using MaIN.Services.Utils;

namespace MaIN.Services.Services.Steps.Commands;

public class FetchCommandHandler(
    IHttpClientFactory httpClientFactory,
    IDataSourceProvider dataSourceService,
    ILLMServiceFactory llmServiceFactory,
    MaINSettings settings) : ICommandHandler<FetchCommand, Message?>
{
    public async Task<Message?> HandleAsync(FetchCommand command)
    {
        var properties = new Dictionary<string, string>
        {
            { "agent_internal", "true" }
        };

        Message? response;

        switch (command.Context.Source!.Type)
        {
            case AgentSourceType.File:
                response = await HandleFileSource(command, properties);
                break;

            case AgentSourceType.Web:
                response = await HandleWebSource(command, properties);
                break;

            case AgentSourceType.Text:
                var textData = dataSourceService.FetchTextData(command.Context.Source.Details);
                response = CreateMessage(textData, properties);
                break;

            case AgentSourceType.API:
                var apiData = await dataSourceService.FetchApiData(
                    command.Context.Source.Details,
                    command.Filter,
                    httpClientFactory,
                    properties);
                response = CreateMessage(apiData, properties);
                break;

            case AgentSourceType.SQL:
                var sqlData = await dataSourceService.FetchSqlData(
                    command.Context.Source.Details,
                    command.Filter,
                    properties);
                response = CreateMessage(sqlData, properties);
                break;

            case AgentSourceType.NoSQL:
                var noSqlData = await dataSourceService.FetchNoSqlData(
                    command.Context.Source.Details,
                    command.Filter,
                    properties);
                response = CreateMessage(noSqlData, properties);
                break;

            default:
                throw new ArgumentOutOfRangeException(command.Context.Source!.Type.ToString());
        }

        // Process JSON response if needed
        if (response.Properties.ContainsValue("JSON"))
        {
            response = await ProcessJsonResponse(response, command);
        }

        return response;
    }

    private async Task<Message> HandleFileSource(FetchCommand command, Dictionary<string, string> properties)
    {
        var fileData = JsonSerializer.Deserialize<AgentFileSourceDetails>(command.Context.Source!.Details?.ToString()!);

        if (command.Chat.Messages.Count > 0)
        {
            var memoryChat = command.MemoryChat;
            var result = await llmServiceFactory.CreateService(command.Chat.Backend ?? settings.BackendType)
                .AskMemory(
                    memoryChat!,
                    new ChatMemoryOptions
                    {
                        FilesData = fileData!.Files,
                        PreProcess = fileData.PreProcess
                    }
                );

            return result!.Message;
        }

        var data = await dataSourceService.FetchFileData(fileData!.Files);
        return CreateMessage(data, properties);
    }

    private async Task<Message> HandleWebSource(FetchCommand command, Dictionary<string, string> properties)
    {
        var webData = JsonSerializer.Deserialize<AgentWebSourceDetails>(command.Context.Source!.Details?.ToString()!);

        if (command.Chat.Messages.Count > 0)
        {
            var memoryChat = command.MemoryChat;
            var result = await llmServiceFactory.CreateService(command.Chat.Backend ?? settings.BackendType)
                .AskMemory(memoryChat!, new ChatMemoryOptions { WebUrls = [webData!.Url] });
            result!.Message.Role = command.ResponseType == FetchResponseType.AS_System ? "System" : "Assistant";
            return result!.Message;
        }

        return CreateMessage($"Web data from {webData!.Url}", properties);
    }

    private async Task<Message> ProcessJsonResponse(Message response, FetchCommand command)
    {
        var chunker = new JsonChunker();
        var chunksAsList = chunker.ChunkJson(response.Content).ToList();
        var chunks = chunksAsList
            .Select((chunk, index) => new { Key = $"CHUNK_{index + 1}-{chunksAsList.Count}", Value = chunk })
            .ToDictionary(item => item.Key, item => item.Value);

        var result = await llmServiceFactory.CreateService(command.Chat.Backend ?? settings.BackendType).AskMemory(command.MemoryChat!, new ChatMemoryOptions
        {
            TextData = chunks
        });

        result!.Message.Role = command.ResponseType == FetchResponseType.AS_System ? "System" : "Assistant";
        var newMessage = result!.Message;
        newMessage.Properties = new() { { "agent_internal", "true" } };
        return newMessage;
    }

    private static Message CreateMessage(string content, Dictionary<string, string> properties)
    {
        return new Message
        {
            Content = content,
            Role = "System",
            Properties = properties
        };
    }
}