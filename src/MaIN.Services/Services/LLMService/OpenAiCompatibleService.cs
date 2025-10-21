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
                    options,
                    cancellationToken);
            }
            else
            {
                await ProcessNonStreamingChatAsync(
                    chat,
                    conversation,
                    apiKey,
                    resultBuilder,
                    options,
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
            var jsonGrammarConverter = new GrammarToJsonConverter();
            var jsonGrammar = jsonGrammarConverter.ConvertToJson(chat.MemoryParams.Grammar);
            userQuery = $"{userQuery} | Respond only using the following JSON format: \n{jsonGrammar}\n. Do not add explanations, code tags, or any extra content.";
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
        ChatRequestOptions options,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var requestBody = BuildRequestBody(chat, conversation, true);
        
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

                        await InvokeTokenCallbackAsync(options.TokenCallback, token);
                        if (token.Type == TokenType.Message)
                        {
                            resultBuilder.Append(token.Text);
                        }

                        if (options.InteractiveUpdates)
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

        var choice = chunk?.Choices?.FirstOrDefault();
        if (choice == null) return null;

        var delta = choice.Delta;
        
        // Handle tool calls in streaming
        if (delta?.ToolCalls != null && delta.ToolCalls.Any())
        {
            return new LLMTokenValue 
            { 
                Text = JsonSerializer.Serialize(delta.ToolCalls), 
                Type = TokenType.ToolCall 
            };
        }

        var value = delta?.Content;
        return string.IsNullOrEmpty(value)
            ? null
            : new LLMTokenValue { Text = value, Type = TokenType.Message };
    }

    private async Task ProcessNonStreamingChatAsync(
        Chat chat,
        List<ChatMessage> conversation,
        string apiKey,
        StringBuilder resultBuilder,
        ChatRequestOptions options,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var requestBody = BuildRequestBody(chat, conversation, false);

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

    private object BuildRequestBody(Chat chat, List<ChatMessage> conversation, bool stream)
    {
        var requestBody = new Dictionary<string, object>
        {
            ["model"] = chat.Model,
            ["messages"] = BuildMessagesArray(conversation, chat, ImageType.AsUrl).Result,
            ["stream"] = stream
        };

        if (chat.Tools != null && chat.Tools.Any())
        {
            requestBody["tools"] = chat.Tools;
            
            if (!string.IsNullOrEmpty(chat.ToolChoice))
            {
                requestBody["tool_choice"] = chat.ToolChoice;
            }
        }

        return requestBody;
    }

    internal static void MergeMessages(List<ChatMessage> conversation, List<Message> messages)
    {
        var existing = new HashSet<(string, object)>(conversation.Select(m => (m.Role, m.Content)));
        foreach (var msg in messages)
        {
            var role = msg.Role.ToLowerInvariant();
        
            if (HasImages(msg))
            {
                var simplifiedContent = $"{msg.Content} [Contains image]";
                if (!existing.Contains((role, simplifiedContent)))
                {
                    var chatMessage = new ChatMessage(role, msg.Content);
                    chatMessage.OriginalMessage = msg;
                    conversation.Add(chatMessage);
                    existing.Add((role, simplifiedContent));
                }
            }
            else
            {
                if (!existing.Contains((role, msg.Content)))
                {
                    conversation.Add(new ChatMessage(role, msg.Content));
                    existing.Add((role, msg.Content));
                }
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

    internal static async Task<object[]> BuildMessagesArray(List<ChatMessage> conversation, Chat chat, ImageType imageType)
    {
        var messages = new List<object>();
    
        foreach (var msg in conversation)
        {
            var content = msg.OriginalMessage != null ? BuildMessageContent(msg.OriginalMessage, imageType) : msg.Content;            
            if (chat.InterferenceParams.Grammar != null && msg.Role == "user")
            {
                var jsonGrammarConverter = new GrammarToJsonConverter();
                string jsonGrammar = jsonGrammarConverter.ConvertToJson(chat.InterferenceParams.Grammar);
                
                var grammarInstruction = $" | Respond only using the following JSON format: \n{jsonGrammar}\n. Do not add explanations, code tags, or any extra content.";
            
                if (content is string textContent)
                {
                    content = textContent + grammarInstruction;
                }
                else if (content is List<object> contentParts)
                {
                    var modifiedParts = contentParts.ToList();
                    modifiedParts.Add(new { type = "text", text = grammarInstruction });
                    content = modifiedParts;
                }
            }

            var messageObj = new Dictionary<string, object>
            {
                ["role"] = msg.Role,
                ["content"] = content
            };

            // Add tool calls if present (for assistant messages)
            if (msg.ToolCalls != null && msg.ToolCalls.Any())
            {
                messageObj["tool_calls"] = msg.ToolCalls;
            }

            // Add tool call id if present (for tool messages)
            if (!string.IsNullOrEmpty(msg.ToolCallId))
            {
                messageObj["tool_call_id"] = msg.ToolCallId;
            }

            messages.Add(messageObj);
        }
    
        return messages.ToArray();
    }
    
    private static async Task InvokeTokenCallbackAsync(Func<LLMTokenValue, Task>? callback, LLMTokenValue token)
    {
        if (callback != null)
        {
            await callback.Invoke(token);
        }
    }
    
    private static bool HasImages(Message message)
    {
        return message.Image != null && message.Image.Length > 0;
    }

    private static object BuildMessageContent(Message message, ImageType imageType)
    {
        if (!HasImages(message))
        {
            return message.Content;
        }

        var contentParts = new List<object>();

        if (!string.IsNullOrEmpty(message.Content))
        {
            contentParts.Add(new
            {
                type = "text",
                text = message.Content
            });
        }

        if (message.Image != null && message.Image.Length > 0)
        {
            var base64Data = Convert.ToBase64String(message.Image);
            var mimeType = DetectImageMimeType(message.Image);

            switch (imageType)
            {
                case ImageType.AsUrl:
                    contentParts.Add(new
                    {
                        type = "image_url",
                        image_url = new
                        {
                            url = $"data:{mimeType};base64,{base64Data}",
                            detail = "auto"
                        }
                    });
                    break;
                case ImageType.AsBase64:
                    contentParts.Add(new
                    {
                        type = "image",
                        source = new
                        {
                            data = base64Data,
                            media_type = mimeType,
                            type = "base64"
                        }
                    });
                    break;
            }
        }

        return contentParts;
    }

    private static string DetectImageMimeType(byte[] imageBytes)
    {
        if (imageBytes.Length < 4)
            return "image/jpeg";

        if (imageBytes[0] == 0xFF && imageBytes[1] == 0xD8)
            return "image/jpeg";
    
        if (imageBytes.Length >= 8 && 
            imageBytes[0] == 0x89 && imageBytes[1] == 0x50 && 
            imageBytes[2] == 0x4E && imageBytes[3] == 0x47)
            return "image/png";
        
        if (imageBytes.Length >= 6 && 
            imageBytes[0] == 0x47 && imageBytes[1] == 0x49 && 
            imageBytes[2] == 0x46 && imageBytes[3] == 0x38)
            return "image/gif";
        
        if (imageBytes.Length >= 12 && 
            imageBytes[0] == 0x52 && imageBytes[1] == 0x49 && 
            imageBytes[2] == 0x46 && imageBytes[3] == 0x46 &&
            imageBytes[8] == 0x57 && imageBytes[9] == 0x45 && 
            imageBytes[10] == 0x42 && imageBytes[11] == 0x50)
            return "image/webp";

        return "image/jpeg";
    }
}

// Tool definition classes
public class ToolDefinition
{
    public string Type { get; set; } = "function";
    public FunctionDefinition Function { get; set; } = null!;
}

public class FunctionDefinition
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public object Parameters { get; set; } = null!;
}

public class ToolCall
{
    public string Id { get; set; } = null!;
    public string Type { get; set; } = "function";
    public FunctionCall Function { get; set; } = null!;
}

public class FunctionCall
{
    public string Name { get; set; } = null!;
    public string Arguments { get; set; } = null!;
}

internal class ChatMessage
{
    public string Role { get; set; }
    public object Content { get; set; }
    public Message? OriginalMessage { get; set; }
    public List<ToolCall>? ToolCalls { get; set; }
    public string? ToolCallId { get; set; }

    public ChatMessage(string role, object content)
    {
        Role = role;
        Content = content;
    }
}

internal enum ImageType
{
    AsUrl,
    AsBase64
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
    public List<ToolCall>? ToolCalls { get; set; }
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
    public List<ToolCall>? ToolCalls { get; set; }
}

file class OpenAiModelsResponse
{ 
    public List<OpenAiModel>? Data { get; set; }
}

file class OpenAiModel
{
    public string? Id { get; set; }
}