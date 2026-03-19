using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Domain.Models.Abstract;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService;
using MaIN.Services.Services.LLMService.Factory;
using MaIN.Services.Services.Models.Commands;
using MaIN.Services.Services.Steps.Commands.Abstract;
using MaIN.Services.Utils;
using System.Text.Json;

namespace MaIN.Services.Services.Steps.Commands;

public class FetchCommandHandler(
    IHttpClientFactory httpClientFactory,
    IDataSourceProvider dataSourceService,
    ILLMServiceFactory llmServiceFactory,
    MaINSettings settings) : ICommandHandler<FetchCommand, Message?>
{
    public async Task<Message?> HandleAsync(FetchCommand command)
    {
        var backend = ModelRegistry.TryGetById(command.Chat.ModelId, out var resolvedModel)
            ? resolvedModel!.Backend
            : settings.BackendType;

        var properties = new Dictionary<string, string>
        {
            { "agent_internal", "true" },
            { Message.UnprocessedMessageProperty, string.Empty }
        };

        Message? response;

        switch (command.Context.Source!.Type)
        {
            case AgentSourceType.File:
                response = await HandleFileSource(command, properties, backend);
                break;

            case AgentSourceType.Web:
                response = await HandleWebSource(command, properties, backend);
                break;

            case AgentSourceType.Text:
                var textData = dataSourceService.FetchTextData(command.Context.Source.Details);
                response = CreateMessage(textData, properties, backend);
                break;

            case AgentSourceType.API:
                var apiData = await dataSourceService.FetchApiData(
                    command.Context.Source.Details,
                    command.Filter,
                    httpClientFactory,
                    properties);
                response = CreateMessage(apiData, properties, backend);
                break;

            case AgentSourceType.SQL:
                var sqlData = await dataSourceService.FetchSqlData(
                    command.Context.Source.Details,
                    command.Filter,
                    properties);
                response = CreateMessage(sqlData, properties, backend);
                break;

            case AgentSourceType.NoSQL:
                var noSqlData = await dataSourceService.FetchNoSqlData(
                    command.Context.Source.Details,
                    command.Filter,
                    properties);
                response = CreateMessage(noSqlData, properties, backend);
                break;

            default:
                throw new ArgumentOutOfRangeException(command.Context.Source!.Type.ToString());
        }

        // Process JSON response if needed
        if (response.Properties.ContainsValue("JSON"))
        {
            response = await ProcessJsonResponse(response, command, backend);
        }

        return response;
    }

    private async Task<Message> HandleFileSource(
        FetchCommand command,
        Dictionary<string, string> properties,
        BackendType backend)
    {
        var fileData = JsonSerializer.Deserialize<AgentFileSourceDetails>(command.Context.Source!.Details?.ToString()!);
        var filesDictionary = fileData!.Files.ToDictionary(path => Path.GetFileName(path), path => path);

        if (command.Chat.Messages.Count > 0)
        {
            var memoryChat = command.MemoryChat;
            var result = await llmServiceFactory.CreateService(backend)
                .AskMemory(
                    memoryChat!,
                    new ChatMemoryOptions
                    {
                        FilesData = filesDictionary,
                        PreProcess = fileData.PreProcess
                    }, new ChatRequestOptions()
                );

            return result!.Message;
        }

        var data = await dataSourceService.FetchFileData(filesDictionary);
        return CreateMessage(data, properties, backend);
    }

    private async Task<Message> HandleWebSource(
        FetchCommand command,
        Dictionary<string, string> properties,
        BackendType backend)
    {
        var webData = JsonSerializer.Deserialize<AgentWebSourceDetails>(command.Context.Source!.Details?.ToString()!);

        if (command.Chat.Messages.Count > 0)
        {
            var memoryChat = command.MemoryChat;
            var result = await llmServiceFactory.CreateService(backend)
                .AskMemory(memoryChat!, new ChatMemoryOptions { WebUrls = [webData!.Url] }, new ChatRequestOptions());
            result!.Message.Role = command.ResponseType == FetchResponseType.AS_System ? "System" : "Assistant";
            return result!.Message;
        }

        return CreateMessage($"Web data from {webData!.Url}", properties, backend);
    }

    private async Task<Message> ProcessJsonResponse(Message response, FetchCommand command, BackendType backend)
    {
        var chunker = new JsonChunker();
        var chunksAsList = chunker.ChunkJson(response.Content).ToList();
        var chunks = chunksAsList
            .Select((chunk, index) => new { Key = $"CHUNK_{index + 1}-{chunksAsList.Count}", Value = chunk })
            .ToDictionary(item => item.Key, item => item.Value);

        var result = await llmServiceFactory
            .CreateService(backend)
            .AskMemory(
                command.MemoryChat!,
                new ChatMemoryOptions
                {
                    TextData = chunks
                },
                new ChatRequestOptions());

        result!.Message.Role = command.ResponseType == FetchResponseType.AS_System ? "System" : "Assistant";
        var newMessage = result!.Message;
        newMessage.Properties = new()
        {
            { "agent_internal", "true" },
            { Message.UnprocessedMessageProperty, string.Empty }
        };
        return newMessage;
    }

    private static Message CreateMessage(string content,
        Dictionary<string, string> properties,
        BackendType backend)
    {
        return new Message
        {
            Content = content,
            Role = "System",
            Properties = properties,
            Type = backend != BackendType.Self ? MessageType.CloudLLM : MessageType.LocalLLM
        };
    }
}
