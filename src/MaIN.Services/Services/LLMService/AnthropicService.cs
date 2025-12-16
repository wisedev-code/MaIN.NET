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
using MaIN.Domain.Entities.Tools;
using MaIN.Services.Services.LLMService.Utils;
using MaIN.Services.Services.LLMService;

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
    private const int MaxToolIterations = 5;

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
            var memoryResult = await AskMemory(chat, result, options, cancellationToken);
            resultBuilder.Append(memoryResult!.Message.Content);
            lastMessage.MarkProcessed();
            UpdateSessionCache(chat.Id, resultBuilder.ToString(), options.CreateSession);
            if (options.TokenCallback != null)
            {
                await options.TokenCallback(new LLMTokenValue()
                {
                    Text = resultBuilder.ToString(),
                    Type = TokenType.FullAnswer
                });
            }
            return CreateChatResult(chat, resultBuilder.ToString(), tokens);
        }

        if (chat.ToolsConfiguration?.Tools != null && chat.ToolsConfiguration.Tools.Any())
        {
            return await ProcessWithToolsAsync(
                chat,
                conversation,
                apiKey,
                tokens,
                options,
                cancellationToken);
        }

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

        return CreateChatResult(chat, resultBuilder.ToString(), tokens);
    }

    private async Task<ChatResult> ProcessWithToolsAsync(
        Chat chat,
        List<ChatMessage> conversation,
        string apiKey,
        List<LLMTokenValue> tokens,
        ChatRequestOptions options,
        CancellationToken cancellationToken)
    {
        StringBuilder resultBuilder = new();
        StringBuilder fullResponseBuilder = new();
        int iterations = 0;
        List<AnthropicToolUse>? currentToolUses = null;

        while (iterations < MaxToolIterations)
        {
            if (iterations > 0 && fullResponseBuilder.Length > 0)
            {
                var spaceToken = new LLMTokenValue { Text = " ", Type = TokenType.Message };
                tokens.Add(spaceToken);
                
                if (options.TokenCallback != null)
                    await options.TokenCallback(spaceToken);

                if (options.InteractiveUpdates)
                {
                    await notificationService.DispatchNotification(
                        NotificationMessageBuilder.CreateChatCompletion(chat.Id, spaceToken, false),
                        ServiceConstants.Notifications.ReceiveMessageUpdate);
                }
            }

            if (options.InteractiveUpdates || options.TokenCallback != null)
            {
                currentToolUses = await ProcessStreamingChatWithToolsAsync(
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
                currentToolUses = await ProcessNonStreamingChatWithToolsAsync(
                    chat,
                    conversation,
                    apiKey,
                    resultBuilder,
                    cancellationToken);
            }

            if (resultBuilder.Length > 0)
            {
                if (fullResponseBuilder.Length > 0)
                {
                    fullResponseBuilder.Append(" ");
                }
                fullResponseBuilder.Append(resultBuilder);
            }

            if (currentToolUses == null || !currentToolUses.Any())
            {
                break;
            }

            var assistantContent = new List<object>();
            if (resultBuilder.Length > 0)
            {
                assistantContent.Add(new { type = "text", text = resultBuilder.ToString() });
            }
            assistantContent.AddRange(currentToolUses.Select(tu => new
            {
                type = "tool_use",
                id = tu.Id,
                name = tu.Name,
                input = tu.Input
            }));

            conversation.Add(new ChatMessage(ServiceConstants.Roles.Assistant, assistantContent));

            var toolResults = new List<object>();
            foreach (var toolUse in currentToolUses)
            {
                if (chat.Properties.CheckProperty(ServiceConstants.Properties.AgentIdProperty))
                {
                    await notificationService.DispatchNotification(
                        NotificationMessageBuilder.ProcessingTools(chat.Properties[ServiceConstants.Properties.AgentIdProperty],
                            string.Empty,
                            toolUse.Name),
                        ServiceConstants.Notifications.ReceiveAgentUpdate);
                }
                
                var executor = chat.ToolsConfiguration?.GetExecutor(toolUse.Name);

                if (executor == null)
                {
                    throw new InvalidOperationException($"No executor found for tool: {toolUse.Name}");
                }

                try
                {
                    var inputJson = JsonSerializer.Serialize(toolUse.Input);
                    options.ToolCallback?.Invoke(new ToolInvocation()
                    {
                        ToolName = toolUse.Name,
                        Arguments = toolUse.Input.ToString() ?? string.Empty,
                        Done = false
                    });
                    var toolResult = await executor(inputJson);
                    options.ToolCallback?.Invoke(new ToolInvocation()
                    {
                        ToolName = toolUse.Name,
                        Arguments = toolUse.Input.ToString() ?? string.Empty,
                        Done = true
                    });

                    toolResults.Add(new
                    {
                        type = "tool_result",
                        tool_use_id = toolUse.Id,
                        content = toolResult
                    });
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error executing tool {ToolName}", toolUse.Name);

                    var errorResult = JsonSerializer.Serialize(new { error = ex.Message });
                    toolResults.Add(new
                    {
                        type = "tool_result",
                        tool_use_id = toolUse.Id,
                        content = errorResult,
                        is_error = true
                    });
                }
            }

            conversation.Add(new ChatMessage(ServiceConstants.Roles.User, toolResults));

            resultBuilder.Clear();
            iterations++;
        }

        if (iterations >= MaxToolIterations)
        {
            logger?.LogWarning("Maximum tool iterations ({MaxIterations}) reached for chat {ChatId}",
                MaxToolIterations, chat.Id);
        }

        var finalResponse = fullResponseBuilder.ToString();
        var finalToken = new LLMTokenValue { Text = finalResponse, Type = TokenType.FullAnswer };
        tokens.Add(finalToken);

        if (options.InteractiveUpdates)
        {
            await notificationService.DispatchNotification(
                NotificationMessageBuilder.CreateChatCompletion(chat.Id, finalToken, true),
                ServiceConstants.Notifications.ReceiveMessageUpdate);
        }

        chat.Messages.Last().MarkProcessed();
        UpdateSessionCache(chat.Id, finalResponse, options.CreateSession);
        return CreateChatResult(chat, finalResponse, tokens);
    }

    private async Task<List<AnthropicToolUse>?> ProcessStreamingChatWithToolsAsync(
        Chat chat,
        List<ChatMessage> conversation,
        string apiKey,
        List<LLMTokenValue> tokens,
        StringBuilder resultBuilder,
        ChatRequestOptions options,
        CancellationToken cancellationToken)
    {
        var httpClient = CreateAnthropicHttpClient();

        var requestBody = BuildAnthropicRequestBody(chat, conversation, true);
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

        var toolUseBuilders = new Dictionary<int, AnthropicToolUseBuilder>();

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith("event:"))
            {
                continue;
            }

            if (line.StartsWith("data:"))
            {
                var data = line.Substring("data:".Length).Trim();

                try
                {
                    var chunk = JsonSerializer.Deserialize<AnthropicStreamChunk>(data,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (chunk?.Type == "content_block_start")
                    {
                        if (chunk.ContentBlock?.Type == "tool_use")
                        {
                            toolUseBuilders[chunk.Index] = new AnthropicToolUseBuilder
                            {
                                Id = chunk.ContentBlock.Id ?? string.Empty,
                                Name = chunk.ContentBlock.Name ?? string.Empty
                            };
                        }
                    }
                    else if (chunk?.Type == "content_block_delta")
                    {
                        if (chunk.Delta?.Type == "text_delta" && !string.IsNullOrEmpty(chunk.Delta.Text))
                        {
                            var token = new LLMTokenValue
                            {
                                Text = chunk.Delta.Text,
                                Type = TokenType.Message
                            };
                            tokens.Add(token);
                            resultBuilder.Append(chunk.Delta.Text);

                            if (options.TokenCallback != null)
                                await options.TokenCallback(token);

                            if (options.InteractiveUpdates)
                            {
                                await notificationService.DispatchNotification(
                                    NotificationMessageBuilder.CreateChatCompletion(chat.Id, token, false),
                                    ServiceConstants.Notifications.ReceiveMessageUpdate);
                            }
                        }
                        else if (chunk.Delta?.Type == "input_json_delta")
                        {
                            if (toolUseBuilders.ContainsKey(chunk.Index) && 
                                !string.IsNullOrEmpty(chunk.Delta.PartialJson))
                            {
                                toolUseBuilders[chunk.Index].InputJson.Append(chunk.Delta.PartialJson);
                            }
                        }
                    }
                    else if (chunk?.Type == "message_stop")
                    {
                        break;
                    }
                }
                catch (JsonException ex)
                {
                    logger?.LogError(ex, "Failed to parse Anthropic chunk");
                }
            }
        }

        if (toolUseBuilders.Any())
        {
            return toolUseBuilders.Values.Select(b => b.Build()).ToList();
        }

        return null;
    }

    private async Task<List<AnthropicToolUse>?> ProcessNonStreamingChatWithToolsAsync(
        Chat chat,
        List<ChatMessage> conversation,
        string apiKey,
        StringBuilder resultBuilder,
        CancellationToken cancellationToken)
    {
        var httpClient = CreateAnthropicHttpClient();

        var requestBody = BuildAnthropicRequestBody(chat, conversation, false);
        var requestJson = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        using var response = await httpClient.PostAsync(CompletionsUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var chatResponse = JsonSerializer.Deserialize<AnthropicMessageResponse>(responseJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var toolUses = new List<AnthropicToolUse>();

        if (chatResponse?.Content != null)
        {
            foreach (var contentBlock in chatResponse.Content)
            {
                if (contentBlock.Type == "text" && !string.IsNullOrEmpty(contentBlock.Text))
                {
                    resultBuilder.Append(contentBlock.Text);
                }
                else if (contentBlock.Type == "tool_use")
                {
                    toolUses.Add(new AnthropicToolUse
                    {
                        Id = contentBlock.Id!,
                        Name = contentBlock.Name!,
                        Input = contentBlock.Input!
                    });
                }
            }
        }

        return toolUses.Any() ? toolUses : null;
    }

    private object BuildAnthropicRequestBody(Chat chat, List<ChatMessage> conversation, bool stream)
    {
        var requestBody = new Dictionary<string, object>
        {
            ["model"] = chat.Model,
            ["max_tokens"] = chat.InterferenceParams.MaxTokens < 0 ? 4096 : chat.InterferenceParams.MaxTokens,
            ["stream"] = stream,
            ["messages"] = BuildAnthropicMessages(conversation)
        };
        
        var systemMessage = conversation.FirstOrDefault(m => 
            m.Role.Equals("system", StringComparison.OrdinalIgnoreCase));
    
        if (systemMessage != null && systemMessage.Content is string systemContent)
        {
            requestBody["system"] = systemContent;
        }

        if (chat.InterferenceParams.Grammar is not null)
        {
            requestBody["system"] = $"Respond only using the following grammar format: \n{chat.InterferenceParams.Grammar.Value}\n. Do not add explanations, code tags, or any extra content.";
        }

        if (chat.ToolsConfiguration?.Tools != null && chat.ToolsConfiguration.Tools.Any())
        {
            requestBody["tools"] = chat.ToolsConfiguration.Tools.Select(t => new
            {
                name = t.Function!.Name,
                description = t.Function.Description,
                input_schema = t.Function.Parameters
            }).ToList();
        }

        return requestBody;
    }

    private List<object> BuildAnthropicMessages(List<ChatMessage> conversation)
    {
        var messages = new List<object>();

        foreach (var msg in conversation)
        {
            if (msg.Role.Equals("system", StringComparison.OrdinalIgnoreCase))
                continue;
            
            object content;

            if (msg.Content is string textContent)
            {
                content = textContent;
            }
            else if (msg.Content is List<object> contentBlocks)
            {
                content = contentBlocks;
            }
            else
            {
                content = msg.Content;
            }

            messages.Add(new
            {
                role = msg.Role,
                content = content
            });
        }

        return messages;
    }

    public async Task<ChatResult?> AskMemory(Chat chat, ChatMemoryOptions memoryOptions, ChatRequestOptions requestOptions, CancellationToken cancellationToken = default)
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
            system = chat.InterferenceParams.Grammar is not null ? $"Respond only using the following grammar format: \n{chat.InterferenceParams.Grammar.Value}\n. Do not add explanations, code tags, or any extra content." : "",
            messages = await OpenAiCompatibleService.BuildMessagesArray(conversation, chat, ImageType.AsBase64)
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

            if (line.StartsWith("data:"))
            {
                var data = line.Substring("data:".Length).Trim();

                try
                {
                    var chunk = JsonSerializer.Deserialize<AnthropicStreamChunk>(data, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (chunk?.Delta?.Type == "text_delta" && !string.IsNullOrEmpty(chunk.Delta.Text))
                    {
                        var token = new LLMTokenValue
                        {
                            Text = chunk.Delta.Text,
                            Type = TokenType.Message
                        };
                        
                        tokens.Add(token);

                        if (tokenCallback != null)
                        {
                            await tokenCallback(token);
                        }

                        resultBuilder.Append(chunk.Delta.Text);

                        if (interactiveUpdates)
                        {
                            await notificationService.DispatchNotification(
                                NotificationMessageBuilder.CreateChatCompletion(chat.Id, token, false),
                                ServiceConstants.Notifications.ReceiveMessageUpdate);
                        }
                    }
                }
                catch (JsonException)
                {
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
        var httpClient = CreateAnthropicHttpClient();

        var requestBody = new
        {
            model = chat.Model,
            max_tokens = chat.InterferenceParams.MaxTokens < 0 ? 4096 : chat.InterferenceParams.MaxTokens,
            stream = false,
            system = chat.InterferenceParams.Grammar is not null ? $"Respond only using the following grammar format: \n{chat.InterferenceParams.Grammar.Value}\n. Do not add explanations, code tags, or any extra content." : "",
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
                Role = nameof(AuthorRole.Assistant),
                Type = MessageType.CloudLLM
            }.MarkProcessed()
        };
    }
}

internal class AnthropicToolUse
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public object Input { get; set; } = null!;
}

internal class AnthropicToolUseBuilder
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public StringBuilder InputJson { get; set; } = new();

    public AnthropicToolUse Build()
    {
        object input;
        var jsonString = InputJson.ToString().Trim();
        
        if (string.IsNullOrWhiteSpace(jsonString))
        {
            input = new { };
        }
        else
        {
            try
            {
                input = JsonSerializer.Deserialize<object>(jsonString) ?? new { };
            }
            catch (JsonException)
            {
                input = new { };
            }
        }
        
        return new AnthropicToolUse
        {
            Id = Id,
            Name = Name,
            Input = input
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
    public string? Text { get; set; }
    public string? Id { get; set; }
    public string? Name { get; set; }
    public object? Input { get; set; }
}

file class AnthropicStreamChunk
{
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("index")]
    public int Index { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("delta")]
    public AnthropicDelta? Delta { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("content_block")]
    public AnthropicContentBlock? ContentBlock { get; set; }
}

file class AnthropicContentBlock
{
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("input")]
    public object? Input { get; set; }
}

file class AnthropicDelta
{
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("text")]
    public string? Text { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("partial_json")]
    public string? PartialJson { get; set; }
}

file class AnthropicModelListResponse
{
    public required List<AnthropicModelInfo> Data { get; set; }
}

file class AnthropicModelInfo
{
    public required string Id { get; set; }
}