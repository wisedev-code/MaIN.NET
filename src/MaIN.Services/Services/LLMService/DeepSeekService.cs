using System.Text;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Services.Services.Abstract;
using Microsoft.Extensions.Logging;
using MaIN.Services.Services.LLMService.Memory;
using MaIN.Services.Constants;
using MaIN.Services.Services.Models;
using MaIN.Domain.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MaIN.Services.Services.LLMService;

public sealed class DeepSeekService(
    MaINSettings settings,
    INotificationService notificationService,
    IHttpClientFactory httpClientFactory,
    IMemoryFactory memoryFactory,
    IMemoryService memoryService,
    ILogger<OpenAiService>? logger = null)
    : OpenAiCompatibleService(notificationService, httpClientFactory, memoryFactory, memoryService, logger)
{
    private readonly MaINSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    private readonly IHttpClientFactory _httpClientFactory =
        httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

    protected override string HttpClientName => ServiceConstants.HttpClients.DeepSeekClient;
    protected override string ChatCompletionsUrl => ServiceConstants.ApiUrls.DeepSeekOpenAiChatCompletions;
    protected override string ModelsUrl => ServiceConstants.ApiUrls.DeepSeekModels;

    protected override string GetApiKey()
    {
        return _settings.DeepSeekKey ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY") ??
            throw new InvalidOperationException("DeepSeek Key not configured");
    }

    protected override void ValidateApiKey()
    {
        if (string.IsNullOrEmpty(_settings.DeepSeekKey) &&
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY")))
        {
            throw new InvalidOperationException("DeepSeek Key not configured");
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
        var result = await Send(chat, new ChatRequestOptions()
        {
            InteractiveUpdates = true
        }, cancellationToken);
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

protected override LLMTokenValue? ProcessChatCompletionChunk(string data)
{
    var chunk = JsonSerializer.Deserialize<ChatCompletionChunk>(data,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    var contentValue = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
    if (!string.IsNullOrEmpty(contentValue))
    {
        return new LLMTokenValue { Text = contentValue, Type = TokenType.Message };
    }

    var reasoningContentValue = chunk?.Choices?.FirstOrDefault()?.Delta?.ReasoningContent;
    if (!string.IsNullOrEmpty(reasoningContentValue))
    {
        return new LLMTokenValue { Text = reasoningContentValue, Type = TokenType.Reason };
    }

    return null;
}

}
file class ChatCompletionChunk
{
    public List<ChoiceChunk>? Choices { get; set; }
}

file class ChoiceChunk
{
    public Delta? Delta { get; set; }
}

file class Delta
{
    [JsonPropertyName("content")] public string? Content { get; set; }

    [JsonPropertyName("reasoning_content")]
    public string? ReasoningContent { get; set; } // property specific for DeepSeek
}