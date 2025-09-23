using System.Text;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Services.Services.Abstract;
using Microsoft.Extensions.Logging;
using MaIN.Services.Services.LLMService.Memory;
using MaIN.Services.Constants;
using MaIN.Services.Services.Models;

namespace MaIN.Services.Services.LLMService;

public sealed class GroqCloudService(
    MaINSettings settings,
    INotificationService notificationService,
    IHttpClientFactory httpClientFactory,
    IMemoryFactory memoryFactory,
    IMemoryService memoryService,
    ILogger<OpenAiService>? logger = null)
    : OpenAiCompatibleService(notificationService, httpClientFactory, memoryFactory, memoryService, logger)
{
    private readonly MaINSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    protected override string HttpClientName => ServiceConstants.HttpClients.GroqCloudClient;
    protected override string ChatCompletionsUrl => ServiceConstants.ApiUrls.GroqCloudOpenAiChatCompletions;
    protected override string ModelsUrl => ServiceConstants.ApiUrls.GroqCloudModels;

    protected override string GetApiKey()
    {
        return _settings.GroqCloudKey ?? Environment.GetEnvironmentVariable("GROQ_API_KEY") ??
            throw new InvalidOperationException("GroqCloud Key not configured");
    }

    protected override void ValidateApiKey()
    {
        if (string.IsNullOrEmpty(_settings.GroqCloudKey) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GROQ_API_KEY")))
        {
            throw new InvalidOperationException("GroqCloud Key not configured");
        }
    }

    public override async Task<ChatResult?> AskMemory(
        Chat chat,
        ChatMemoryOptions memoryOptions,
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
