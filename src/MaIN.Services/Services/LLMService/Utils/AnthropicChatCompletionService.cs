using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using MaIN.Services.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace MaIN.Services.Services.LLMService.Utils;

public sealed class AnthropicChatCompletionService : IChatCompletionService
{
    private readonly string _model;
    private readonly HttpClient _httpClient;
    private const string CompletionsUrl = ServiceConstants.ApiUrls.AnthropicChatMessages;
    public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();
    private readonly ILogger<AnthropicChatCompletionService> _logger;

    public AnthropicChatCompletionService(ILogger<AnthropicChatCompletionService> logger, IHttpClientFactory httpClientFactory, string model, string apiKey)
    {
        _logger = logger;
        _model = model;
        _httpClient = httpClientFactory.CreateClient(ServiceConstants.HttpClients.AnthropicClient);
        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        var tools = GetToolsFromKernel(kernel);
        const int maxToolLoops = 15;

        for(var i = 0; i < maxToolLoops; i++)
        {
            using var doc = await SendAnthropicRequestAsync(chatHistory, tools, executionSettings, cancellationToken);
            var contentArray = doc.RootElement.GetProperty("content").EnumerateArray().ToList();
            var stopReason = doc.RootElement.GetProperty("stop_reason").GetString();

            chatHistory.AddAssistantMessage(JsonSerializer.Serialize(contentArray));

            foreach (var contentItem in contentArray)
            {
                var type = contentItem.GetProperty("type").GetString();
                switch (type)
                {
                    case "text":
                    {
                        if (stopReason == "end_turn")
                        {
                            var content = contentItem.GetProperty("text").GetString() ?? "";
                            var result = new ChatMessageContent(AuthorRole.Assistant, content, modelId: _model);
                            return new List<ChatMessageContent> { result };
                        }

                        break;
                    }

                    case "tool_use":
                    {
                        if (kernel == null)
                        {
                            throw new InvalidOperationException("Anthropic requested a tool call but no Kernel was provided.");
                        }

                        var toolName = contentItem.GetProperty("name").GetString();
                        var toolInput = contentItem.GetProperty("input").ToString();
                        var toolCallId = contentItem.GetProperty("id").GetString();

                        var pluginFunc = kernel.Plugins
                            .SelectMany(p => p)
                            .FirstOrDefault(f => f.Metadata.Name == toolName);

                        if (pluginFunc == null)
                        {
                            throw new InvalidOperationException($"Tool '{toolName}' not found.");
                        }

                        var functionArgs = JsonSerializer.Deserialize<Dictionary<string, object>>(toolInput);
                        var functionResult = await pluginFunc.InvokeAsync(kernel, new KernelArguments(functionArgs!), cancellationToken);

                        var toolResultPayload = new[]
                        {
                            new Dictionary<string, object?>
                            {
                                ["type"] = "tool_result",
                                ["tool_use_id"] = toolCallId,
                                ["content"] = ExtractToolText(functionResult)
                            }
                        };
                        var contentJson = JsonSerializer.Serialize(toolResultPayload);

                        // Anthropic expects tool_result payloads to come from the user role.
                        chatHistory.AddMessage(AuthorRole.User, contentJson);
                        break;
                    }

                    default:
                    {
                        _logger.LogError("Not implemented Anthropic message type handling: {type}.", type);
                        return new List<ChatMessageContent>();
                    }
                }
            }
        }

        throw new InvalidOperationException("Anthropic did not produce a final text response within allowed tool_call loops.");
    }

    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // When using tools (tool_use → tool_result → text), Anthropic does not return partial text responses incrementally.
        // Therefore, streaming token-by-token in real time is unnecessary and not useful in this case.
        // Anthropic typically returns a single final message.

        var results = await GetChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken);
        foreach (var result in results)
        {
            yield return new StreamingChatMessageContent(result.Role, result.Content, result.ModelId);
        }
    }

    private async Task<JsonDocument> SendAnthropicRequestAsync(
        ChatHistory chatHistory,
        List<object> tools,
        PromptExecutionSettings? executionSettings,
        CancellationToken cancellationToken)
    {
        var messages = chatHistory.Select(m => new
        {
            role = m.Role.ToString().ToLowerInvariant(),
            content = m.Content
        }).ToList();

        var body = new
        {
            model = _model,
            messages,
            max_tokens = executionSettings?.ExtensionData?["max_tokens"] ?? 1024,
            stream = false,
            tools = tools.Count > 0 ? tools : null
        };

        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, CompletionsUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonDocument.Parse(responseJson);
    }


    private static List<object> GetToolsFromKernel(Kernel? kernel)
    {
        if (kernel == null) return new();

        var tools = kernel.Plugins
            .Where(x => x.Name == "Tools")
            .SelectMany(p => p)
            .Select(f => new
            {
                name = f.Metadata.Name,
                description = f.Metadata.Description,
                input_schema = ToJsonSchema(f.Metadata?.Parameters)
            }).ToList();

        return tools.Cast<object>().ToList();
    }

    public static object ToJsonSchema(IReadOnlyList<KernelParameterMetadata>? parameters)
    {
        if (parameters == null || parameters.Count == 0)
        {
            return "{}";
        }

        var jsonProperties = new Dictionary<string, object>();

        foreach (var param in parameters)
        {
            jsonProperties[param.Name] = new
            {
                type = MapToJsonSchemaType(param.ParameterType!),
                description = param.Description
            };
        }

        var schema = new
        {
            type = "object",
            properties = jsonProperties,
            required = parameters
                .Where(p => p.IsRequired)
                .Select(p => p.Name)
                .ToList()
        };

        return schema;
    }

    private static string MapToJsonSchemaType(Type type) =>
        type switch
        {
            _ when type == typeof(string) => "string",
            _ when type == typeof(int) || type == typeof(long) || type == typeof(float) || type == typeof(double) || type == typeof(decimal) => "number",
            _ when type == typeof(bool) => "boolean",
            _ when typeof(IEnumerable<object>).IsAssignableFrom(type) => "array",
            _ when type == typeof(object) => "object",
            _ => "string"
        };

    private static string ExtractToolText(FunctionResult result)
    {
        try
        {
            var json = result.GetValue<JsonElement>();

            if (json.TryGetProperty("content", out var contentProp) &&
                contentProp.ValueKind == JsonValueKind.Array &&
                contentProp.GetArrayLength() > 0 &&
                contentProp[0].TryGetProperty("text", out var textProp))
            {
                return textProp.GetString() ?? "";
            }

            return json.ToString();
        }
        catch
        {
            return result.GetValue<object>()?.ToString() ?? "";
        }
    }
}