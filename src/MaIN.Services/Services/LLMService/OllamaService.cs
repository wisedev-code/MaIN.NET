using System.Text;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService.Memory;
using MaIN.Services.Services.Models;
using Microsoft.Extensions.Logging;

namespace MaIN.Services.Services.LLMService;

public sealed class OllamaService(
    MaINSettings settings,
    INotificationService notificationService,
    IHttpClientFactory httpClientFactory,
    IMemoryFactory memoryFactory,
    IMemoryService memoryService,
    ILogger<OpenAiService>? logger = null)
    : OpenAiCompatibleService(notificationService, httpClientFactory, memoryFactory, memoryService, logger)
{
    private readonly MaINSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    private bool HasApiKey => !string.IsNullOrEmpty(_settings.OllamaKey) || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OLLAMA_API_KEY"));

    protected override string HttpClientName => HasApiKey ? ServiceConstants.HttpClients.OllamaClient : ServiceConstants.HttpClients.OllamaLocalClient;
    protected override string ChatCompletionsUrl => HasApiKey ? ServiceConstants.ApiUrls.OllamaOpenAiChatCompletions : ServiceConstants.ApiUrls.OllamaLocalOpenAiChatCompletions;
    protected override string ModelsUrl => HasApiKey ? ServiceConstants.ApiUrls.OllamaModels : ServiceConstants.ApiUrls.OllamaLocalModels;

    protected override string GetApiKey()
    {
        return _settings.OllamaKey ?? Environment.GetEnvironmentVariable("OLLAMA_API_KEY") ?? string.Empty;
    }

    protected override void ValidateApiKey()
    {
        // No validation required - local Ollama doesn't need an API key
        // Cloud Ollama will fail at runtime if the key is missing
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