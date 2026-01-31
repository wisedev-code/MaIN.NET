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

        return ExtractFromCodeBlock(text)
               ?? FindBalancedJson(text)
               ?? ExtractPartialJson(text);
    }

    private static string? ExtractFromCodeBlock(string text)
    {
        var patterns = new[]
        {
            @"```json\s*([\s\S]*?)(?:```|$)",
            @"```\s*([\s\S]*?)(?:```|$)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern);
            if (match.Success)
            {
                var content = match.Groups[1].Value.Trim();
                content = RemoveXmlTags(content);

                var balanced = FindBalancedJson(content);
                if (balanced != null)
                    return balanced;
            }
        }

        return null;
    }

    private static string RemoveXmlTags(string content)
    {
        content = Regex.Replace(content, @"^<[^>]+>\s*", "");
        content = Regex.Replace(content, @"\s*</[^>]+>$", "");
        content = Regex.Replace(content, @"^<\w+\s*$", "");
        content = Regex.Replace(content, @"^\s*</?\w+\s*$", "", RegexOptions.Multiline);
        content = Regex.Replace(content, @"</?\w+\s*$", "");

        return content.Trim();
    }

    private static string? FindBalancedJson(string text)
    {
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

    private static string? ExtractPartialJson(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '{' || text[i] == '[')
            {
                char openChar = text[i];
                char closeChar = openChar == '{' ? '}' : ']';

                var (json, balanced) = ExtractWithCompletion(text, i, openChar, closeChar);

                if (json != null)
                {
                    if (balanced)
                        return json;

                    var completed = TryCompleteJson(json, openChar, closeChar);
                    if (completed != null && IsLikelyValidJson(completed))
                        return completed;
                }
            }
        }

        return null;
    }

    private static (string? json, bool balanced) ExtractWithCompletion(string text, int startIndex, char openChar,
        char closeChar)
    {
        int depth = 0;
        bool inString = false;
        bool escaped = false;
        int lastValidPosition = startIndex;

        for (int i = startIndex; i < text.Length; i++)
        {
            char c = text[i];

            if (escaped)
            {
                escaped = false;
                lastValidPosition = i;
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
                lastValidPosition = i;
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
                    lastValidPosition = i;

                    if (depth == 0)
                    {
                        return (text.Substring(startIndex, i - startIndex + 1), true);
                    }
                }
                else if (char.IsLetterOrDigit(c) || c == ':' || c == ',' || char.IsWhiteSpace(c))
                {
                    lastValidPosition = i;
                }
            }
            else
            {
                lastValidPosition = i;
            }
        }

        if (lastValidPosition > startIndex)
        {
            return (text.Substring(startIndex, lastValidPosition - startIndex + 1), false);
        }

        return (null, false);
    }

    private static string? TryCompleteJson(string json, char openChar, char closeChar)
    {
        int depth = 0;
        bool inString = false;
        bool escaped = false;

        foreach (char c in json)
        {
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

            if (c == '"')
            {
                inString = !inString;
            }
            else if (!inString)
            {
                if (c == openChar) depth++;
                else if (c == closeChar) depth--;
            }
        }

        if (inString)
        {
            json += "\"";
            depth++;
        }

        if (depth > 0)
        {
            json += new string(closeChar, depth);
        }

        return json;
    }

    private static bool IsValidJsonStart(string json)
    {
        json = json.Trim();
        return (json.StartsWith("{") && json.EndsWith("}")) ||
               (json.StartsWith("[") && json.EndsWith("]"));
    }

    private static bool IsLikelyValidJson(string json)
    {
        try
        {
            json = json.Trim();
            if (!IsValidJsonStart(json))
                return false;

            int braceCount = 0, bracketCount = 0;
            bool inString = false;
            bool escaped = false;

            foreach (char c in json)
            {
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

                if (c == '"')
                {
                    inString = !inString;
                }
                else if (!inString)
                {
                    if (c == '{') braceCount++;
                    else if (c == '}') braceCount--;
                    else if (c == '[') bracketCount++;
                    else if (c == ']') bracketCount--;
                }
            }

            return braceCount == 0 && bracketCount == 0 && !inString;
        }
        catch
        {
            return false;
        }
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
        [JsonPropertyName("tool_calls")] public List<ToolCall>? ToolCalls { get; init; }
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