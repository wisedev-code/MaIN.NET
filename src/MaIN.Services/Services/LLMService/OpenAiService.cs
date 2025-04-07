using LLama.Common;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;
using MaIN.Services.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using MaIN.Services.Constants;

namespace MaIN.Services.Services.LLMService;

public class OpenAiService(
    MaINSettings settings,
    INotificationService notificationService,
    IHttpClientFactory httpClientFactory,
    ILogger<OpenAiService>? logger = null)
    : ILLMService, IDisposable
{
    private readonly MaINSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    private readonly INotificationService _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private static readonly ConcurrentDictionary<string, List<ChatMessage>> SessionCache = new();
    private bool _disposed;
    
    public async Task<ChatResult?> SendAsync(
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

        if (HasFiles(chat.Messages.Last()))
        {
            var result = await ProcessFilesAsync(chat, cancellationToken);
            resultBuilder.Append(result?.Message.Content);
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
        
        // Add final token and send notification if needed
        var finalToken = new LLMTokenValue { Text = resultBuilder.ToString(), Type = TokenType.FullAnswer };
        tokens.Add(finalToken);
        
        if (options.InteractiveUpdates)
        {
            await _notificationService.DispatchNotification(
                NotificationMessageBuilder.CreateChatCompletion(chat.Id, finalToken, true),
                ServiceConstants.Notifications.ReceiveMessageUpdate);
        }

        // Update session cache if needed
        UpdateSessionCache(chat.Id, resultBuilder.ToString(), options.CreateSession);

        return CreateChatResult(chat, resultBuilder.ToString(), tokens);
    }

    /// <summary>
    /// Legacy method for compatibility - delegates to the new async method
    /// </summary>
    public async Task<ChatResult?> Send(
        Chat chat,
        bool interactiveUpdates = false,
        bool createSession = false,
        Func<LLMTokenValue, Task>? changeOfValue = null)
    {
        var options = new ChatRequestOptions
        {
            InteractiveUpdates = interactiveUpdates,
            CreateSession = createSession,
            TokenCallback = changeOfValue
        };
        
        return await SendAsync(chat, options);
    }

    private async Task<ChatResult?> AskMemoryAsync(
        Chat chat,
        ChatMemoryOptions memoryOptions,
        CancellationToken cancellationToken = default)
    {
        if (!chat.Messages.Any())
            return null;

        var kernelMemory = new KernelMemoryBuilder()
            .WithOpenAIDefaults(GetApiKey())
            .Build();

        await ImportDataToMemory(kernelMemory, memoryOptions, cancellationToken);

        var userQuery = chat.Messages.Last().Content;
        var retrievedContext = await kernelMemory.AskAsync(userQuery, cancellationToken: cancellationToken);

        await kernelMemory.DeleteIndexAsync(cancellationToken: cancellationToken);
        return CreateChatResult(chat, retrievedContext.Result, []);
    }
    
    public async Task<ChatResult?> AskMemory(
        Chat chat,
        Dictionary<string, string>? textData = null,
        Dictionary<string, string>? fileData = null,
        Dictionary<string, FileStream>? streamData = null,
        List<string>? webUrls = null,
        List<string>? memory = null)
    {
        var options = new ChatMemoryOptions
        {
            TextData = textData,
            FileData = fileData,
            StreamData = streamData,
            WebUrls = webUrls,
            Memory = memory
        };
        
        return await AskMemoryAsync(chat, options);
    }

    /// <summary>
    /// Gets the current available models from OpenAI.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetCurrentModelsAsync(CancellationToken cancellationToken = default)
    {
        ValidateApiKey();
        
        var client = _httpClientFactory.CreateClient(ServiceConstants.HttpClients.OpenAiClient);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", 
            GetApiKey());

        using var response = await client.GetAsync(ServiceConstants.ApiUrls.OpenAiModels, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var modelsResponse = JsonSerializer.Deserialize<OpenAiModelsResponse>(responseJson);

        return modelsResponse?.Data?
            .Where(m => m.Id!.StartsWith("gpt-", StringComparison.InvariantCultureIgnoreCase))
            .Select(m => m.Id)
            .Where(id => id != null)
            .Cast<string>()
            .ToList() 
            ?? new List<string>();
    }

    /// <summary>
    /// Legacy method for compatibility - delegates to the new async method
    /// </summary>
    public async Task<List<string?>> GetCurrentModels()
    {
        var models = await GetCurrentModelsAsync();
        return models.Cast<string?>().ToList();
    }

    /// <summary>
    /// Cleans the session cache for a specific chat ID.
    /// </summary>
    public Task CleanSessionCacheAsync(string id)
    {
        SessionCache.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Legacy method for compatibility - delegates to the new async method
    /// </summary>
    public Task CleanSessionCache(string id) => CleanSessionCacheAsync(id);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Nothing to dispose in this class
        }

        _disposed = true;
    }

    #region Private Methods

    private string GetApiKey()
    {
        return _settings.OpenAiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? 
               throw new InvalidOperationException("OpenAi Key not configured");
    }

    private void ValidateApiKey()
    {
        if (string.IsNullOrEmpty(_settings.OpenAiKey) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY")))
        {
            throw new InvalidOperationException("OpenAi Key not configured");
        }
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

    private async Task<ChatResult?> ProcessFilesAsync(Chat chat, CancellationToken cancellationToken)
    {
        var lastMessage = chat.Messages.Last();
        
        var textData = lastMessage.Files!
            .Where(x => x.Content is not null)
            .ToDictionary(x => x.Name, x => x.Content!);
        
        var fileData = lastMessage.Files!
            .Where(x => x.Path is not null)
            .ToDictionary(x => x.Name, x => x.Path!);
        
        var streamData = lastMessage.Files!
            .Where(x => x.StreamContent is not null)
            .ToDictionary(x => x.Name, x => x.StreamContent!);

        var memoryOptions = new ChatMemoryOptions
        {
            TextData = textData,
            FileData = fileData,
            StreamData = streamData
        };
        
        return await AskMemoryAsync(chat, memoryOptions, cancellationToken);
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
        var client = _httpClientFactory.CreateClient(ServiceConstants.HttpClients.OpenAiClient);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        
        var requestBody = new
        {
            model = chat.Model,
            messages = conversation.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            stream = true
        };

        var requestJson = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(requestJson, Encoding.UTF8, MediaTypeNames.Application.Json);
        
        using var request = new HttpRequestMessage(HttpMethod.Post, ServiceConstants.ApiUrls.OpenAiChatCompletions)
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
                    var chunk = JsonSerializer.Deserialize<ChatCompletionChunk>(data,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                    var value = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
                    if (!string.IsNullOrEmpty(value))
                    {
                        var token = new LLMTokenValue { Text = value, Type = TokenType.Message };
                        tokens.Add(token);
                        
                        await InvokeTokenCallbackAsync(tokenCallback, token);
                        resultBuilder.Append(value);
                        
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

    private async Task ProcessNonStreamingChatAsync(
        Chat chat,
        List<ChatMessage> conversation, 
        string apiKey,
        StringBuilder resultBuilder,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(ServiceConstants.HttpClients.OpenAiClient);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        
        var requestBody = new
        {
            model = chat.Model,
            messages = conversation.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            stream = false
        };

        var requestJson = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(requestJson, Encoding.UTF8, MediaTypeNames.Application.Json);
        
        using var response = await client.PostAsync(ServiceConstants.ApiUrls.OpenAiChatCompletions, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var chatResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson);
        var responseContent = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content;
        
        if (responseContent != null)
        {
            resultBuilder.Append(responseContent);
        }
    }

    private async Task ImportDataToMemory(
        IKernelMemory kernelMemory, 
        ChatMemoryOptions options,
        CancellationToken cancellationToken)
    {
        if (options.TextData != null)
        {
            foreach (var item in options.TextData)
            {
                await kernelMemory.ImportTextAsync(item.Value, item.Key, cancellationToken: cancellationToken);
            }
        }

        if (options.FileData != null)
        {
            foreach (var item in options.FileData)
            {
                try
                {
                    await kernelMemory.ImportDocumentAsync(item.Value, item.Key, cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error importing file '{FileName}'", item.Key);
                }
            }
        }
        
        if (options.StreamData != null)
        {
            foreach (var item in options.StreamData)
            {
                await kernelMemory.ImportDocumentAsync(item.Value, item.Key, cancellationToken: cancellationToken);
            }
        }
        
        if (options.WebUrls != null)
        {
            foreach (var url in options.WebUrls)
            {
                await kernelMemory.ImportWebPageAsync(url, cancellationToken: cancellationToken);
            }
        }

        if (options.Memory != null)
        {
            for (int i = 0; i < options.Memory.Count; i++)
            {
                await kernelMemory.ImportTextAsync(options.Memory[i], $"memory_{i + 1}", cancellationToken: cancellationToken);
            }
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
                Role = AuthorRole.Assistant.ToString()
            }
        };
    }

    private static async Task InvokeTokenCallbackAsync(Func<LLMTokenValue, Task>? callback, LLMTokenValue token)
    {
        if (callback != null)
        {
            await callback.Invoke(token);
        }
    }

    #endregion
}

/// <summary>
/// Options for chat requests to the OpenAI API.
/// </summary>
public class ChatRequestOptions
{
    /// <summary>
    /// Whether to provide interactive updates during the chat completion.
    /// </summary>
    public bool InteractiveUpdates { get; set; }
    
    /// <summary>
    /// Whether to create a session for this chat.
    /// </summary>
    public bool CreateSession { get; set; }
    
    /// <summary>
    /// Callback function for token updates.
    /// </summary>
    public Func<LLMTokenValue, Task>? TokenCallback { get; set; }
}

/// <summary>
/// Options for memory-based chat requests.
/// </summary>
public class ChatMemoryOptions
{
    /// <summary>
    /// Dictionary of text data to process.
    /// </summary>
    public Dictionary<string, string>? TextData { get; set; }
    
    /// <summary>
    /// Dictionary of file paths to process.
    /// </summary>
    public Dictionary<string, string>? FileData { get; set; }
    
    /// <summary>
    /// Dictionary of file streams to process.
    /// </summary>
    public Dictionary<string, FileStream>? StreamData { get; set; }
    
    /// <summary>
    /// List of web URLs to process.
    /// </summary>
    public List<string>? WebUrls { get; set; }
    
    /// <summary>
    /// List of memory items to process.
    /// </summary>
    public List<string>? Memory { get; set; }
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

file abstract class OpenAiModel
{
    public string? Id { get; set; }
}
