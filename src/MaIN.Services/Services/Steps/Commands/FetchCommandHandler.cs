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
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MaIN.Services.Services.Steps.Commands;

public class FetchCommandHandler(
    IHttpClientFactory httpClientFactory,
    IDataSourceProvider dataSourceService,
    ILLMServiceFactory llmServiceFactory,
    MaINSettings settings,
    INotificationService notificationService) : ICommandHandler<FetchCommand, Message?>
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

    private static string GetDetailsJson(object? details) => details switch
    {
        string s => s,
        null => "{}",
        var obj => JsonSerializer.Serialize(obj)
    };

    private async Task<Message> HandleFileSource(
        FetchCommand command,
        Dictionary<string, string> properties,
        BackendType backend)
    {
        var fileData = JsonSerializer.Deserialize<AgentFileSourceDetails>(GetDetailsJson(command.Context.Source!.Details));
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
        var webData = JsonSerializer.Deserialize<AgentWebSourceDetails>(GetDetailsJson(command.Context.Source!.Details));

        if (command.Chat.Messages.Count > 0)
        {
            var memoryChat = command.MemoryChat;
            var client = httpClientFactory.CreateClient();
            var rawContent = await client.GetStringAsync(webData!.Url);
            var cleanText = ExtractCleanWebText(rawContent, webData.Url);

            var agentId = command.Chat.Properties.TryGetValue("AgentId", out var aid) ? aid : command.Chat.Id;

            await notificationService.DispatchNotification(
                NotificationMessageBuilder.CreateActorProgress(
                    agentId, "true", "FETCH",
                    "Default",
                    $"URL={webData.Url} rawBytes={rawContent?.Length ?? 0} cleanLen={cleanText?.Length ?? 0}"),
                "ReceiveAgentUpdate");

            await notificationService.DispatchNotification(
                NotificationMessageBuilder.CreateActorProgress(
                    agentId, "true", "FETCH",
                    "Default",
                    $"cleanPreview={Truncate(cleanText, 300)}"),
                "ReceiveAgentUpdate");

            var result = await llmServiceFactory.CreateService(backend)
                .AskMemory(memoryChat!, new ChatMemoryOptions { TextData = new Dictionary<string, string> { ["web-content"] = cleanText } }, new ChatRequestOptions());

            await notificationService.DispatchNotification(
                NotificationMessageBuilder.CreateActorProgress(
                    agentId, "true", "FETCH",
                    "Default",
                    $"AskMemory.len={result?.Message?.Content?.Length ?? 0} preview={Truncate(result?.Message?.Content, 300)}"),
                "ReceiveAgentUpdate");

            result!.Message.Role = command.ResponseType == FetchResponseType.AS_System ? "System" : "Assistant";
            return result!.Message;
        }

        await notificationService.DispatchNotification(
            NotificationMessageBuilder.CreateActorProgress(
                command.Chat.Id, "true", "FETCH",
                "Default",
                $"chat empty, placeholder for URL={webData!.Url}"),
            "ReceiveAgentUpdate");
        return CreateMessage($"Web data from {webData!.Url}", properties, backend);
    }

    private static string Truncate(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "<empty>" : (s.Length <= max ? s : s[..max] + "…");

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

    private static string ExtractCleanWebText(string rawContent, string url)
    {
        var trimmed = rawContent.TrimStart();
        if (trimmed.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("<rss", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("<feed", StringComparison.OrdinalIgnoreCase))
        {
            return ExtractRssText(rawContent);
        }

        return StripHtmlTags(rawContent);
    }

    private static string ExtractRssText(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            var items = doc.Descendants()
                .Where(e => e.Name.LocalName is "item" or "entry")
                .Take(30)
                .Select(item =>
                {
                    var title = item.Descendants()
                        .FirstOrDefault(e => e.Name.LocalName == "title")?.Value?.Trim();
                    var desc = item.Descendants()
                        .FirstOrDefault(e => e.Name.LocalName is "description" or "summary")?.Value?.Trim();
                    var pubDate = item.Descendants()
                        .FirstOrDefault(e => e.Name.LocalName is "pubDate" or "published" or "updated")?.Value?.Trim();
                    var parts = new[] { title, pubDate, desc }
                        .Where(p => !string.IsNullOrWhiteSpace(p));
                    return string.Join(" | ", parts);
                })
                .Where(s => !string.IsNullOrWhiteSpace(s));

            return string.Join("\n", items);
        }
        catch
        {
            return StripHtmlTags(xml);
        }
    }

    private static string StripHtmlTags(string html)
    {
        var noScript = Regex.Replace(html, @"<script[^>]*>.*?</script>", " ", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var noStyle = Regex.Replace(noScript, @"<style[^>]*>.*?</style>", " ", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var noTags = Regex.Replace(noStyle, @"<[^>]+>", " ");
        var clean = Regex.Replace(noTags, @"\s{2,}", " ").Trim();
        return clean.Length > 8000 ? clean[..8000] : clean;
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
