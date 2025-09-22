using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;
using MaIN.Services.Utils;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text;
using LLama.Common;
using MaIN.Domain.Configuration;
using MaIN.Services.Services.LLMService.Utils;

namespace MaIN.Services.Services.LLMService;

public sealed class AnthropicService(
    MaINSettings settings,
    INotificationService notificationService,
    IHttpClientFactory httpClientFactory,
    ILogger<AnthropicService>? logger = null)
    : ILLMService
{
    private readonly MaINSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    private static readonly ConcurrentDictionary<string, List<ChatMessage>> SessionCache = new();

    private const string CompletionsUrl = ServiceConstants.ApiUrls.AnthropicChatMessages;
    private const string ModelsUrl = ServiceConstants.ApiUrls.AnthropicModels;

    private HttpClient CreateAnthropicHttpClient()
    {
        var client = httpClientFactory.CreateClient(ServiceConstants.HttpClients.AnthropicClient);
        client.DefaultRequestHeaders.Add("x-api-key", GetApiKey());
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        return client;
    }

    private string GetApiKey()
    {
        return _settings.AnthropicKey ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ??
            throw new InvalidOperationException("Anthropic Key not configured");
    }

    private void ValidateApiKey()
    {
        if (string.IsNullOrEmpty(_settings.AnthropicKey) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")))
        {
            throw new InvalidOperationException("Anthropic Key not configured");
        }
    }

    public async Task<ChatResult?> Send(Chat chat, ChatRequestOptions options, CancellationToken cancellationToken = default)
    {
        ValidateApiKey();

        if (!chat.Messages.Any())
            return null;

        var apiKey = GetApiKey();
        var conversation = GetOrCreateConversation(chat, options.CreateSession);
        var resultBuilder = new StringBuilder();
        var tokens = new List<LLMTokenValue>();

        var lastMessage = chat.Messages.Last();

        if (HasFiles(lastMessage))
        {
            var result = ChatHelper.ExtractMemoryOptions(lastMessage);
            var memoryResult = await AskMemory(chat, result, cancellationToken);
            resultBuilder.Append(memoryResult!.Message.Content);
        }
        else
        {
            if (options.InteractiveUpdates || options.TokenCallback != null)
            {
                await ProcessStreamingChatAsync(
                    chat,
                    conversation,
                    apiKey,
                    tokens,
                    resultBuilder,
                    options.TokenCallback,
                    options.InteractiveUpdates,
                    cancellationToken);
            }
            else
            {
                await ProcessNonStreamingChatAsync(
                    chat,
                    conversation,
                    apiKey,
                    resultBuilder,
                    cancellationToken);
            }
        }

        var finalToken = new LLMTokenValue { Text = resultBuilder.ToString(), Type = TokenType.FullAnswer };
        tokens.Add(finalToken);

        if (options.InteractiveUpdates)
        {
            await notificationService.DispatchNotification(
                NotificationMessageBuilder.CreateChatCompletion(chat.Id, finalToken, true),
                ServiceConstants.Notifications.ReceiveMessageUpdate);
        }

        lastMessage.MarkProcessed();
        UpdateSessionCache(chat.Id, resultBuilder.ToString(), options.CreateSession);
        chat.Messages.Last().MarkProcessed();

        return CreateChatResult(chat, resultBuilder.ToString(), tokens);
    }


    public async Task<ChatResult?> AskMemory(Chat chat, ChatMemoryOptions memoryOptions, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Embeddings are not supported by the Anthropic. Document reading requires embedding support.");
    }

    public async Task<string[]> GetCurrentModels()
    {
        ValidateApiKey();
        var httpClient = CreateAnthropicHttpClient();

        using var response = await httpClient.GetAsync(ModelsUrl);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var modelResponse = JsonSerializer.Deserialize<AnthropicModelListResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return modelResponse?.Data?.Select(m => m.Id).ToArray() ?? [];
    }

    public Task CleanSessionCache(string id)
    {
        SessionCache.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    private List<ChatMessage> GetOrCreateConversation(Chat chat, bool createSession)
    {
        List<ChatMessage> conversation;
        if (createSession)
        {
            conversation = SessionCache.GetOrAdd(chat.Id, _ => new List<ChatMessage>());
        }
        else
        {
            conversation = new List<ChatMessage>();
        }

        OpenAiCompatibleService.MergeMessages(conversation, chat.Messages);
        return conversation;
    }
    

    private void UpdateSessionCache(string chatId, string assistantResponse, bool createSession)
    {
        if (createSession && SessionCache.TryGetValue(chatId, out var messages))
        {
            messages.Add(new ChatMessage(ServiceConstants.Roles.Assistant, assistantResponse));
        }
    }

    private static bool HasFiles(Message message)
    {
        return message.Files != null && message.Files.Count > 0;
    }

    private async Task ProcessStreamingChatAsync(
    Chat chat,
    List<ChatMessage> conversation,
    string apiKey,
    List<LLMTokenValue> tokens,
    StringBuilder resultBuilder,
    Func<LLMTokenValue, Task>? tokenCallback,
    bool interactiveUpdates,
    CancellationToken cancellationToken)
    {
        var httpClient = CreateAnthropicHttpClient();

        var requestBody = new
        {
            model = chat.Model,
            max_tokens = chat.InterferenceParams.MaxTokens < 0 ? 4096 : chat.InterferenceParams.MaxTokens,
            stream = true,
            system = chat.InterferenceParams.Grammar is not null ? $"Respond only using the following grammar format: \n{chat.InterferenceParams.Grammar}\n. Do not add explanations, code tags, or any extra content." : "",
            messages = await OpenAiCompatibleService.BuildMessagesArray(conversation, chat, ImageType.AsBase64)
            //todo: Add thinking support
        };

        var requestJson = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, CompletionsUrl)
        {
            Content = content
        };

        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith("data: "))
            {
                var data = line["data: ".Length..].Trim();
                if (data == "[DONE]")
                    break;

                try
                {
                    var token = ProcessAnthropicStreamChunk(data);
                    if (token is not null)
                    {
                        tokens.Add(token);

                        if (tokenCallback != null)
                        {
                            await tokenCallback(token);
                        }

                        if (token.Type == TokenType.Message)
                        {
                            resultBuilder.Append(token.Text);
                        }

                        if (interactiveUpdates)
                        {
                            await notificationService.DispatchNotification(
                                NotificationMessageBuilder.CreateChatCompletion(chat.Id, token, false),
                                ServiceConstants.Notifications.ReceiveMessageUpdate);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Failed to parse Anthropic chunk: {Chunk}", data);
                }
            }
        }
    }

    
    private LLMTokenValue? ProcessAnthropicStreamChunk(string data)
    {
        var chunk = JsonSerializer.Deserialize<AnthropicStreamChunk>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var textDelta = chunk?.Delta?.Text;

        return string.IsNullOrEmpty(textDelta)
            ? null
            : new LLMTokenValue
            {
                Text = textDelta,
                Type = TokenType.Message
            };
    }

    private async Task ProcessNonStreamingChatAsync(
        Chat chat,
        List<ChatMessage> conversation,
        string apiKey,
        StringBuilder resultBuilder,
        CancellationToken cancellationToken)
    {
        var httpClient = CreateAnthropicHttpClient();

        var requestBody = new
        {
            model = chat.Model,
            max_tokens = chat.InterferenceParams.MaxTokens < 0 ? 4096 : chat.InterferenceParams.MaxTokens,
            stream = false,
            system = chat.InterferenceParams.Grammar is not null ? $"Respond only using the following grammar format: \n{chat.InterferenceParams.Grammar}\n. Do not add explanations, code tags, or any extra content." : "",
            messages = await OpenAiCompatibleService.BuildMessagesArray(conversation, chat, ImageType.AsBase64)
        };

        var requestJson = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        using var response = await httpClient.PostAsync(CompletionsUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var chatResponse = JsonSerializer.Deserialize<AnthropicMessageResponse>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var message = chatResponse?.Content?.FirstOrDefault()?.Text;
        if (!string.IsNullOrWhiteSpace(message))
        {
            resultBuilder.Append(message);
        }
    }

    private static ChatResult CreateChatResult(Chat chat, string content, List<LLMTokenValue> tokens)
    {
        return new ChatResult
        {
            Done = true,
            CreatedAt = DateTime.UtcNow,
            Model = chat.Model,
            Message = new Message
            {
                Content = content,
                Tokens = tokens,
                Role = AuthorRole.Assistant.ToString(),
                Type = MessageType.LocalLLM
            }.MarkProcessed()
        };
    }
}

file class AnthropicMessageResponse
{
    public List<AnthropicMessageContent> Content { get; set; } = [];
}

file class AnthropicMessageContent
{
    public string Type { get; set; } = default!;
    public string Text { get; set; } = default!;
}

file class AnthropicStreamChunk
{
    public AnthropicDelta? Delta { get; set; }
}

file class AnthropicDelta
{
    public string? Text { get; set; }
}


file class AnthropicModelListResponse
{
    public List<AnthropicModelInfo> Data { get; set; }
}

file class AnthropicModelInfo
{
    public string Id { get; set; }
}
