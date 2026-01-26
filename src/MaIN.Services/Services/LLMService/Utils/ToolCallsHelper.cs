using System.Text.Json;
using System.Text.Json.Serialization;
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
        text = text.Trim();

        var firstBrace = text.IndexOf('{');
        var firstBracket = text.IndexOf('[');
        var startIndex = (firstBrace >= 0 && firstBracket >= 0) 
            ? Math.Min(firstBrace, firstBracket) 
            : Math.Max(firstBrace, firstBracket);

        var lastBrace = text.LastIndexOf('}');
        var lastBracket = text.LastIndexOf(']');
        var endIndex = Math.Max(lastBrace, lastBracket);

        if (startIndex >= 0 && endIndex > startIndex)
            return text.Substring(startIndex, endIndex - startIndex + 1);

        return null;
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
