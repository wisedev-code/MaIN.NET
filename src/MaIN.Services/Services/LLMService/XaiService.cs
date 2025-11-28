using MaIN.Domain.Configuration;
using MaIN.Services.Constants;
using MaIN.Services.Services.Models;
using MaIN.Domain.Entities;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService.Memory;
using Microsoft.Extensions.Logging;
using System.Text;

namespace MaIN.Services.Services.LLMService;

public sealed class XaiService(
    MaINSettings settings,
    INotificationService notificationService,
    IHttpClientFactory httpClientFactory,
    IMemoryFactory memoryFactory,
    IMemoryService memoryService,
    ILogger<OpenAiService>? logger = null)
    : OpenAiCompatibleService(notificationService, httpClientFactory, memoryFactory, memoryService, logger)
{
    private readonly MaINSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    protected override string HttpClientName => ServiceConstants.HttpClients.XaiClient;
    protected override string ChatCompletionsUrl => ServiceConstants.ApiUrls.XaiOpenAiChatCompletions;
    protected override string ModelsUrl => ServiceConstants.ApiUrls.XaiModels;

    protected override string GetApiKey()
    {
        return _settings.XaiKey ?? Environment.GetEnvironmentVariable("XAI_API_KEY") ??
            throw new InvalidOperationException("xAI Key not configured");
    }

    protected override void ValidateApiKey()
    {
        if (string.IsNullOrEmpty(_settings.XaiKey) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("XAI_API_KEY")))
        {
            throw new InvalidOperationException("xAI Key not configured");
        }
    }

    public override async Task<ChatResult?> AskMemory(
        Chat chat,
        ChatMemoryOptions memoryOptions,
        ChatRequestOptions requestOptions,
        CancellationToken cancellationToken = default)
    {
        var lastMsg = chat.Messages.Last();
        var filePaths = await DocumentProcessor.ConvertToFilesContent(memoryOptions);
        var message = new Message()
        {
            Role = ServiceConstants.Roles.User,
            Content = ComposeMessage(lastMsg, filePaths),
            Type = MessageType.CloudLLM
        };

        chat.Messages.Last().Content = message.Content;
        chat.Messages.Last().Files = [];
        var result = await Send(chat, new ChatRequestOptions(), cancellationToken);
        chat.Messages.Last().Content = lastMsg.Content;
        return result;
    }

    private string ComposeMessage(Message lastMsg, string[] filePaths)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"== FILES IN MEMORY");
        foreach (var path in filePaths)
        {
            var doc = DocumentProcessor.ProcessDocument(path);
            stringBuilder.Append(doc);
            stringBuilder.AppendLine();
        }
        stringBuilder.AppendLine($"== END OF FILES");
        stringBuilder.AppendLine();
        stringBuilder.Append(lastMsg.Content);
        return stringBuilder.ToString();
    }
}