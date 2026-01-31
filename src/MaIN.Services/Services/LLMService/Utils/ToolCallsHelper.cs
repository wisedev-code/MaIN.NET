using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using MaIN.Domain.Entities.Tools;

namespace MaIN.Services.Services.LLMService.Utils;

public static class ToolCallParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static ToolParseResult ParseToolCalls(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return ToolParseResult.Failure("Response is empty.");

        var jsonContent = ExtractJsonContent(response);

        if (string.IsNullOrEmpty(jsonContent))
            return ToolParseResult.ToolNotFound();

        try
        {
            var wrapper = JsonSerializer.Deserialize<ToolResponseWrapper>(jsonContent, JsonOptions);

            if (wrapper?.ToolCalls is not null && wrapper.ToolCalls.Count != 0)
                return ToolParseResult.Success(NormalizeToolCalls(wrapper.ToolCalls));

            return ToolParseResult.Failure("JSON parsed correctly but 'tool_calls' property is missing or empty.");
        }
        catch (JsonException ex)
        {
            return ToolParseResult.Failure($"Invalid JSON format: {ex.Message}");
        }
    }

private static string? ExtractJsonContent(string text)
{
    if (string.IsNullOrWhiteSpace(text))
        return null;

    text = text.Trim();

    var jsonFromCodeBlock = ExtractFromCodeBlock(text);
    if (jsonFromCodeBlock != null)
        return jsonFromCodeBlock;

    return FindBalancedJson(text);
}

private static string? ExtractFromCodeBlock(string text)
{
    var patterns = new[]
    {
        @"```json\s*([\s\S]*?)\s*```",
        @"```\s*([\s\S]*?)\s*```"
    };

    foreach (var pattern in patterns)
    {
        var match = Regex.Match(text, pattern);
        if (match.Success)
        {
            var content = match.Groups[1].Value.Trim();
            
            content = Regex.Replace(content, @"^<[^>]+>\s*", "");
            content = Regex.Replace(content, @"\s*<[^>]+>$", "");
            content = content.Trim();
            
            if (content.StartsWith("{") || content.StartsWith("["))
            {
                var balanced = FindBalancedJson(content);
                if (balanced != null)
                    return balanced;
            }
        }
    }

    return null;
}

private static string? FindBalancedJson(string text)
{
    // Try to find a balanced JSON object or array
    for (int i = 0; i < text.Length; i++)
    {
        if (text[i] == '{')
        {
            var json = ExtractBalanced(text, i, '{', '}');
            if (json != null && IsValidJsonStart(json))
                return json;
        }
        else if (text[i] == '[')
        {
            var json = ExtractBalanced(text, i, '[', ']');
            if (json != null && IsValidJsonStart(json))
                return json;
        }
    }

    return null;
}

private static string? ExtractBalanced(string text, int startIndex, char openChar, char closeChar)
{
    int depth = 0;
    bool inString = false;
    bool escaped = false;

    for (int i = startIndex; i < text.Length; i++)
    {
        char c = text[i];

        if (escaped)
        {
            escaped = false;
            continue;
        }

        if (c == '\\')
        {
            escaped = true;
            continue;
        }

        if (c == '"' && !inString)
        {
            inString = true;
        }
        else if (c == '"' && inString)
        {
            inString = false;
        }
        else if (!inString)
        {
            if (c == openChar)
            {
                depth++;
            }
            else if (c == closeChar)
            {
                depth--;
                if (depth == 0)
                {
                    return text.Substring(startIndex, i - startIndex + 1);
                }
            }
        }
    }

    return null;
}

private static bool IsValidJsonStart(string json)
{
    json = json.Trim();
    return (json.StartsWith("{") && json.EndsWith("}")) ||
           (json.StartsWith("[") && json.EndsWith("]"));
}

    private static List<ToolCall> NormalizeToolCalls(List<ToolCall>? calls)
    {
        if (calls is null)
            return [];

        var normalizedCalls = new List<ToolCall>();
        
        foreach (var call in calls)
        {
            var id = string.IsNullOrEmpty(call.Id) ? Guid.NewGuid().ToString()[..8] : call.Id;
            var type = string.IsNullOrEmpty(call.Type) ? "function" : call.Type;
            var function = call.Function ?? new FunctionCall();
            
            normalizedCalls.Add(call with { Id = id, Type = type, Function = function });
        }
        
        return normalizedCalls;
    }

    private sealed record ToolResponseWrapper
    {
        [JsonPropertyName("tool_calls")]
        public List<ToolCall>? ToolCalls { get; init; }
    }
}

public record ToolParseResult
{
    public bool IsSuccess { get; init; }
    public List<ToolCall>? ToolCalls { get; init; }
    public string? ErrorMessage { get; init; }

    public static ToolParseResult Success(List<ToolCall> calls) => new() { IsSuccess = true, ToolCalls = calls };
    public static ToolParseResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
    public static ToolParseResult ToolNotFound() => new() { IsSuccess = false };
}
