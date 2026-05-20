using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Exceptions;
using MaIN.Domain.Models;
using MaIN.Domain.Models.Concrete;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService.Utils;
using MaIN.Services.Services.Models;
using Microsoft.Extensions.Logging;
using MaIN.Services.Services.LLMService.Memory;
using MaIN.Domain.Configuration.BackendInferenceParams;

namespace MaIN.Services.Services.LLMService;

public sealed class OpenAiService(
    MaINSettings settings,
    INotificationService notificationService,
    IHttpClientFactory httpClientFactory,
    IMemoryFactory memoryFactory,
    IMemoryService memoryService,
    ILogger<OpenAiService>? logger = null)
    : OpenAiCompatibleService(notificationService, httpClientFactory, memoryFactory, memoryService, logger)
{
    private readonly MaINSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<OpenAiService>? _logger = logger;

    protected override Type ExpectedParamsType => typeof(OpenAiInferenceParams);

    protected override string GetApiKey()
    {
        return _settings.OpenAiKey ?? Environment.GetEnvironmentVariable(LLMApiRegistry.OpenAi.ApiKeyEnvName) ??
            throw new APIKeyNotConfiguredException(LLMApiRegistry.OpenAi.ApiName);
    }

    protected override string GetApiName() => LLMApiRegistry.OpenAi.ApiName;

    protected override void ValidateApiKey()
    {
        if (string.IsNullOrEmpty(_settings.OpenAiKey) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable(LLMApiRegistry.OpenAi.ApiKeyEnvName)))
        {
            throw new APIKeyNotConfiguredException(LLMApiRegistry.OpenAi.ApiName);
        }
    }

    protected override void ApplyBackendParams(Dictionary<string, object> requestBody, Chat chat)
    {
        if (chat.BackendParams is not OpenAiInferenceParams p) return;
        if (p.MaxTokens.HasValue) requestBody["max_tokens"] = p.MaxTokens.Value;
        if (p.Temperature.HasValue) requestBody["temperature"] = p.Temperature.Value;
        if (p.TopP.HasValue) requestBody["top_p"] = p.TopP.Value;
        if (p.FrequencyPenalty.HasValue) requestBody["frequency_penalty"] = p.FrequencyPenalty.Value;
        if (p.PresencePenalty.HasValue) requestBody["presence_penalty"] = p.PresencePenalty.Value;
        if (p.ResponseFormat != null) requestBody["response_format"] = new { type = p.ResponseFormat };
    }

    public override async Task<string[]> GetCurrentModels()
    {
        var allModels = await base.GetCurrentModels();

        return allModels
            .Where(id => id.StartsWith("gpt-", StringComparison.InvariantCultureIgnoreCase))
            .ToArray();
    }

    public override async Task<ChatResult?> Send(Chat chat, ChatRequestOptions options, CancellationToken cancellationToken = default)
    {
        var providerSkills = chat.ProviderSkillReferences
            .Where(r => r.Backend == BackendType.OpenAi)
            .ToList();

        if (providerSkills.Count == 0)
            return await base.Send(chat, options, cancellationToken);

        ValidateApiKey();
        if (!chat.Messages.Any())
            return null;

        // Reuse base pre-flight: image extraction, RAG short-circuit, session conversation.
        var lastMessage = chat.Messages.Last();
        await ExtractImageFromFiles(lastMessage);

        if (HasFiles(lastMessage))
        {
            // Match base.Send semantics: RAG short-circuits before any LLM call. AskMemory is
            // expected to return non-null; mirror the base's assumption rather than silently
            // falling through to a Responses call that doesn't know how to handle file payloads.
            var memoryOpts = ChatHelper.ExtractMemoryOptions(lastMessage);
            var memoryResult = await AskMemory(chat, memoryOpts, options, cancellationToken)
                ?? throw new InvalidOperationException(
                    "AskMemory returned null while the last message has files. Provider-skill Responses path requires RAG resolution before submission.");

            lastMessage.MarkProcessed();
            UpdateSessionCache(chat.Id, memoryResult.Message.Content, options.CreateSession);
            return memoryResult;
        }

        var conversation = GetOrCreateConversation(chat, options.CreateSession);
        var result = await SendViaResponsesApi(chat, conversation, providerSkills, cancellationToken);

        lastMessage.MarkProcessed();
        UpdateSessionCache(chat.Id, result.Message.Content, options.CreateSession);
        return result;
    }

    private async Task<ChatResult> SendViaResponsesApi(
        Chat chat,
        List<MaIN.Services.Services.LLMService.ChatMessage> conversation,
        List<MaIN.Domain.Entities.Skills.ProviderSkillReference> skills,
        CancellationToken cancellationToken)
    {
        var apiKey = GetApiKey();
        var client = _httpClientFactory.CreateClient(ServiceConstants.HttpClients.OpenAiClient);

        var systemMessage = conversation.FirstOrDefault(m =>
            string.Equals(m.Role, ServiceConstants.Roles.System, StringComparison.OrdinalIgnoreCase));
        var input = BuildResponsesInput(conversation, systemMessage);

        var requestBody = BuildResponsesRequestBody(chat, skills, input, systemMessage);

        var responseJson = await RunResponsesToolLoopAsync(client, requestBody, input, chat, apiKey, cancellationToken);

        var text = ExtractResponsesOutputText(responseJson);
        _logger?.LogInformation(
            "OpenAI Responses API returned {Chars} chars with provider skills [{Skills}].",
            text.Length, string.Join(", ", skills.Select(s => s.Name)));

        return BuildBatchChatResult(chat, text);
    }

    private static Dictionary<string, object> BuildResponsesRequestBody(
        Chat chat,
        List<MaIN.Domain.Entities.Skills.ProviderSkillReference> skills,
        List<object> input,
        MaIN.Services.Services.LLMService.ChatMessage? systemMessage)
    {
        var skillRefs = skills.Select(BuildSkillReference).ToList();

        // Mixed mode: shell tool carrying provider skill refs + any local-bucket function tools
        // (e.g. code-defined skills with C# Execute delegates). Both kinds can coexist in tools[].
        var toolsArr = new List<object>
        {
            new
            {
                type = ServiceConstants.OpenAiResponses.ShellToolType,
                environment = new
                {
                    type = ServiceConstants.OpenAiResponses.ContainerAuto,
                    skills = skillRefs
                }
            }
        };

        if (chat.ToolsConfiguration?.Tools != null && chat.ToolsConfiguration.Tools.Any())
        {
            toolsArr.AddRange(chat.ToolsConfiguration.Tools.Select(t => (object)new
            {
                type = ServiceConstants.OpenAiResponses.FunctionToolType,
                name = t.Function!.Name,
                description = t.Function.Description,
                parameters = t.Function.Parameters
            }));
        }

        var requestBody = new Dictionary<string, object>
        {
            ["model"] = chat.ModelId,
            ["input"] = input,
            ["tools"] = toolsArr
        };

        // Responses API has a separate top-level `instructions` for the system prompt; the system
        // message is lifted out of input rather than passed inline.
        if (systemMessage?.Content is string sysContent && !string.IsNullOrWhiteSpace(sysContent))
            requestBody["instructions"] = sysContent;

        if (chat.BackendParams is OpenAiInferenceParams p)
        {
            if (p.MaxTokens.HasValue) requestBody["max_output_tokens"] = p.MaxTokens.Value;
            if (p.Temperature.HasValue) requestBody["temperature"] = p.Temperature.Value;
            if (p.TopP.HasValue) requestBody["top_p"] = p.TopP.Value;
        }

        return requestBody;
    }

    private static object BuildSkillReference(MaIN.Domain.Entities.Skills.ProviderSkillReference s)
    {
        var dict = new Dictionary<string, object>
        {
            ["type"] = ServiceConstants.OpenAiResponses.SkillReferenceType,
            ["skill_id"] = s.SkillId
        };

        // Per OpenAI Skills spec `version` is an integer when pinned. Strings like "latest" are
        // conveyed by omitting the field entirely (server resolves to default_version).
        if (!string.IsNullOrEmpty(s.Version) &&
            int.TryParse(s.Version, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var versionInt))
        {
            dict["version"] = versionInt;
        }

        return dict;
    }

    private async Task<string> RunResponsesToolLoopAsync(
        HttpClient client,
        Dictionary<string, object> requestBody,
        List<object> input,
        Chat chat,
        string apiKey,
        CancellationToken cancellationToken)
    {
        const int maxIterations = 5;
        string responseJson = string.Empty;

        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
            using var request = new HttpRequestMessage(HttpMethod.Post, ServiceConstants.ApiUrls.OpenAiResponses)
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var response = await client.SendAsync(request, cancellationToken);
            responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"OpenAI Responses API call failed ({(int)response.StatusCode}): {responseJson}");

            var pending = ExtractPendingFunctionCalls(responseJson);
            if (pending.Count == 0)
                break;

            // Mirror the model's function_call items in input plus their results so the next
            // request has the full chain (without relying on previous_response_id continuity).
            foreach (var call in pending)
            {
                var executor = chat.ToolsConfiguration?.GetExecutor(call.Name);
                var output = executor is not null
                    ? await executor(call.Arguments)
                    : $"Tool '{call.Name}' has no executor.";

                input.Add(new
                {
                    type = ServiceConstants.OpenAiResponses.FunctionCallType,
                    call_id = call.CallId,
                    name = call.Name,
                    arguments = call.Arguments
                });

                input.Add(new
                {
                    type = ServiceConstants.OpenAiResponses.FunctionCallOutputType,
                    call_id = call.CallId,
                    output
                });
            }

            requestBody["input"] = input;
        }

        return responseJson;
    }

    private static ChatResult BuildBatchChatResult(Chat chat, string text)
    {
        var tokens = new List<LLMTokenValue>
        {
            new() { Text = text, Type = TokenType.FullAnswer }
        };

        return new ChatResult
        {
            Done = true,
            CreatedAt = DateTime.UtcNow,
            Model = chat.ModelId,
            Message = new Message
            {
                Content = text,
                Tokens = tokens,
                Role = LLama.Common.AuthorRole.Assistant.ToString(),
                Type = MessageType.LocalLLM
            }.MarkProcessed()
        };
    }

    /// <summary>
    /// Translates Chat-Completions-shaped conversation history into Responses API input items.
    /// Plain user/assistant turns become {role, content}; assistant turns carrying tool_calls
    /// emit a function_call item per call; tool result messages emit function_call_output items.
    /// </summary>
    private static List<object> BuildResponsesInput(List<ChatMessage> conversation, ChatMessage? systemMessage)
    {
        var input = new List<object>();

        foreach (var m in conversation)
        {
            if (ReferenceEquals(m, systemMessage)) continue;

            var role = string.IsNullOrEmpty(m.Role) ? ServiceConstants.Roles.User : m.Role.ToLowerInvariant();

            // Tool result message (role=tool) → function_call_output, referencing the call_id
            // the assistant emitted earlier in the conversation.
            if (string.Equals(role, ServiceConstants.Roles.Tool, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrEmpty(m.ToolCallId))
            {
                input.Add(new
                {
                    type = ServiceConstants.OpenAiResponses.FunctionCallOutputType,
                    call_id = m.ToolCallId,
                    output = m.Content?.ToString() ?? string.Empty
                });
                continue;
            }

            // Assistant turn with tool_calls → optional text content, then one function_call item per call.
            if (string.Equals(role, ServiceConstants.Roles.Assistant, StringComparison.OrdinalIgnoreCase)
                && m.ToolCalls is { Count: > 0 })
            {
                var assistantText = m.Content?.ToString();
                if (!string.IsNullOrWhiteSpace(assistantText))
                    input.Add(new { role, content = assistantText });

                foreach (var call in m.ToolCalls)
                {
                    input.Add(new
                    {
                        type = ServiceConstants.OpenAiResponses.FunctionCallType,
                        call_id = call.Id,
                        name = call.Function.Name,
                        arguments = call.Function.Arguments
                    });
                }
                continue;
            }

            input.Add(new { role, content = m.Content?.ToString() ?? string.Empty });
        }

        return input;
    }

    private sealed record PendingFunctionCall(string CallId, string Name, string Arguments);

    private static List<PendingFunctionCall> ExtractPendingFunctionCalls(string responseJson)
    {
        var calls = new List<PendingFunctionCall>();

        using var doc = JsonDocument.Parse(responseJson);
        if (!doc.RootElement.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
            return calls;

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("type", out var typeEl)) continue;
            if (typeEl.GetString() != ServiceConstants.OpenAiResponses.FunctionCallType) continue;

            var callId = item.TryGetProperty("call_id", out var cidEl) ? cidEl.GetString() : null;
            var name = item.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
            // arguments may arrive as a JSON-encoded string OR as a nested object — handle both.
            string? args = null;
            if (item.TryGetProperty("arguments", out var argsEl))
            {
                args = argsEl.ValueKind == JsonValueKind.String
                    ? argsEl.GetString()
                    : argsEl.GetRawText();
            }

            if (callId is not null && name is not null)
                calls.Add(new PendingFunctionCall(callId, name, args ?? "{}"));
        }

        return calls;
    }

    private static string ExtractResponsesOutputText(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        if (root.TryGetProperty("output_text", out var ot) && ot.ValueKind == JsonValueKind.String)
            return ot.GetString() ?? string.Empty;

        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var contentArr) || contentArr.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var part in contentArr.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
                    sb.Append(t.GetString());
            }
        }

        return sb.ToString();
    }
}
