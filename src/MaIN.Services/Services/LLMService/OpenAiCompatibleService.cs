using MaIN.Domain.Models;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService.Utils;
using MaIN.Services.Services.Models;
using MaIN.Services.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using MaIN.Domain.Entities;
using MaIN.Services.Services.LLMService.Memory;
using LLama.Common;

namespace MaIN.Services.Services.LLMService;

public abstract class OpenAiCompatibleService(
    INotificationService notificationService,
    IHttpClientFactory httpClientFactory,
    IMemoryFactory memoryFactory,
    IMemoryService memoryService,
    ILogger<OpenAiCompatibleService>? logger = null)
    : ILLMService
{
    private readonly INotificationService _notificationService =
        notificationService ?? throw new ArgumentNullException(nameof(notificationService));

    private readonly IHttpClientFactory _httpClientFactory =
        httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

    private static readonly ConcurrentDictionary<string, List<ChatMessage>> SessionCache = new();

    private static readonly JsonSerializerOptions DefaultJsonSerializerOptions =
        new() { PropertyNameCaseInsensitive = true };

    protected abstract string GetApiKey();
    protected abstract void ValidateApiKey();
    protected virtual string HttpClientName => ServiceConstants.HttpClients.OpenAiClient;
    protected virtual string ChatCompletionsUrl => ServiceConstants.ApiUrls.OpenAiChatCompletions;
    protected virtual string ModelsUrl => ServiceConstants.ApiUrls.OpenAiModels;

    public async Task<ChatResult?> Send(
        Chat chat,
        ChatRequestOptions options,
        CancellationToken cancellationToken = default)
    {
        ValidateApiKey();
        if (!chat.Messages.Any())
            return null;

        List<LLMTokenValue> tokens = new();
        string apiKey = GetApiKey();

        List<ChatMessage> conversation = GetOrCreateConversation(chat, options.CreateSession);
        StringBuilder resultBuilder = new();

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
            await _notificationService.DispatchNotification(
                NotificationMessageBuilder.CreateChatCompletion(chat.Id, finalToken, true),
                ServiceConstants.Notifications.ReceiveMessageUpdate);
        }

        lastMessage.MarkProcessed();
        UpdateSessionCache(chat.Id, resultBuilder.ToString(), options.CreateSession);
        return CreateChatResult(chat, resultBuilder.ToString(), tokens);
    }

    public virtual async Task<ChatResult?> AskMemory(
        Chat chat,
        ChatMemoryOptions memoryOptions,
        CancellationToken cancellationToken = default)
    {
        if (!chat.Messages.Any())
            return null;

        var kernel = memoryFactory.CreateMemoryWithOpenAi(GetApiKey(), chat.MemoryParams);

        await memoryService.ImportDataToMemory((kernel, null), memoryOptions, cancellationToken);

        var userQuery = chat.Messages.Last().Content;
        if (chat.MemoryParams.Grammar != null)
        {
            var jsonGrammarConverter = new GBNFToJsonConverter();
            var jsonGrammar = jsonGrammarConverter.ConvertToJson(chat.MemoryParams.Grammar);
            userQuery =
                $"{userQuery} | For your next response only, please respond using exactly the following JSON format: \n{jsonGrammar}\n. Do not include any explanations, code blocks, or additional content. After this single JSON response, resume your normal conversational style.";
        }

        var retrievedContext = await kernel.AskAsync(userQuery, cancellationToken: cancellationToken);

        await kernel.DeleteIndexAsync(cancellationToken: cancellationToken);
        return CreateChatResult(chat, retrievedContext.Result, []);
    }

    public virtual async Task<string[]> GetCurrentModels()
    {
        ValidateApiKey();

        var client = _httpClientFactory.CreateClient(HttpClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            GetApiKey());

        using var response = await client.GetAsync(ModelsUrl);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var modelsResponse = JsonSerializer.Deserialize<OpenAiModelsResponse>(responseJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return (modelsResponse?.Data?
                    .Select(m => m.Id)
                    .Where(id => id != null)
                    .ToArray()
                ?? [])!;
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
            conversation = SessionCache.GetOrAdd(chat.Id, new List<ChatMessage>());
        }
        else
        {
            conversation = new List<ChatMessage>();
        }

        MergeMessages(conversation, chat.Messages);
        return conversation;
    }

    private void UpdateSessionCache(string chatId, string assistantResponse, bool createSession)
    {
        if (createSession && SessionCache.TryGetValue(chatId, out var history))
        {
            history.Add(new ChatMessage(ServiceConstants.Roles.Assistant, assistantResponse));
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
        var client = _httpClientFactory.CreateClient(HttpClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var requestBody = new
        {
            model = chat.Model,
            messages = conversation.Select(m => new
            {
                role = m.Role,
                content = chat.InterferenceParams.Grammar != null
                    //I know that this is a bit ugly, but hey, it works
                    ? $"{m.Content} | Respond only using the following JSON format: \n{new GBNFToJsonConverter().ConvertToJson(chat.InterferenceParams.Grammar)}\n. Do not add explanations, code tags, or any extra content."
                    : m.Content
            }).ToArray(),
            stream = true
        };

        var requestJson = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(requestJson, Encoding.UTF8, MediaTypeNames.Application.Json);

        using var request = new HttpRequestMessage(HttpMethod.Post, ChatCompletionsUrl)
        {
            Content = content
        };

        using var response = await client.SendAsync(
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
                var data = line.Substring("data: ".Length).Trim();
                if (data == "[DONE]")
                    break;

                try
                {
                    var token = ProcessChatCompletionChunk(data);

                    if (token is not null)
                    {
                        tokens.Add(token);

                        await InvokeTokenCallbackAsync(tokenCallback, token);
                        if (token.Type == TokenType.Message)
                        {
                            resultBuilder.Append(token.Text);
                        }

                        if (interactiveUpdates)
                        {
                            await _notificationService.DispatchNotification(
                                NotificationMessageBuilder.CreateChatCompletion(chat.Id, token, false),
                                ServiceConstants.Notifications.ReceiveMessageUpdate);
                        }
                    }
                }
                catch (JsonException ex)
                {
                    logger?.LogError(ex, "Failed to parse chunk: {Data}", data);
                }
            }
        }
    }

    protected virtual LLMTokenValue? ProcessChatCompletionChunk(string data)
    {
        var chunk = JsonSerializer.Deserialize<ChatCompletionChunk>(data,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var value = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;

        return string.IsNullOrEmpty(value)
            ? null
            : new LLMTokenValue { Text = value, Type = TokenType.Message };
    }

    private async Task ProcessNonStreamingChatAsync(
        Chat chat,
        List<ChatMessage> conversation,
        string apiKey,
        StringBuilder resultBuilder,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var requestBody = new
        {
            model = chat.Model,
            messages = conversation.Select(m => new
            {
                role = m.Role, content = chat.InterferenceParams.Grammar != null
                    //I know that this is a bit ugly, but hey, it works
                    ? $"{m.Content} | Respond only using the following JSON format: \n{new GBNFToJsonConverter().ConvertToJson(chat.InterferenceParams.Grammar)}\n. Do not add explanations, code tags, or any extra content."
                    : m.Content
            }).ToArray(),
            stream = false
        };

        var requestJson = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(requestJson, Encoding.UTF8, MediaTypeNames.Application.Json);

        using var response = await client.PostAsync(ChatCompletionsUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var chatResponse =
            JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson, DefaultJsonSerializerOptions);
        var responseContent = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content;

        if (responseContent != null)
        {
            resultBuilder.Append(responseContent);
        }
    }

    private void MergeMessages(List<ChatMessage> conversation, List<Message> messages)
    {
        var existing = new HashSet<(string, string)>(conversation.Select(m => (m.Role, m.Content)));
        foreach (var msg in messages)
        {
            var role = msg.Role.ToLowerInvariant();
            if (!existing.Contains((role, msg.Content)))
            {
                conversation.Add(new ChatMessage(role, msg.Content));
                existing.Add((role, msg.Content));
            }
        }
    }

    protected static ChatResult CreateChatResult(Chat chat, string content, List<LLMTokenValue> tokens)
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

    private static async Task InvokeTokenCallbackAsync(Func<LLMTokenValue, Task>? callback, LLMTokenValue token)
    {
        if (callback != null)
        {
            await callback.Invoke(token);
        }
    }
}

public class ChatRequestOptions
{
    public bool InteractiveUpdates { get; set; }
    public bool CreateSession { get; set; }
    public bool SaveConv { get; set; } = true;
    public Func<LLMTokenValue, Task>? TokenCallback { get; set; }
}

internal class ChatMessage(string role, string content)
{
    public string Role { get; set; } = role;
    public string Content { get; set; } = content;
}

file class ChatCompletionResponse
{
    public List<Choice>? Choices { get; set; }
}

file class Choice
{
    public ChatMessageResponse? Message { get; set; }
}

file class ChatMessageResponse
{
    public string? Content { get; set; }
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
    public string? Content { get; set; }
}

file class OpenAiModelsResponse
{
    public List<OpenAiModel>? Data { get; set; }
}

file class OpenAiModel
{
    public string? Id { get; set; }
}