using System.Text.Json;
using System.Text.Json.Serialization;

namespace MaIN.Domain.Entities.Tools;

public sealed record ToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = "function";

    [JsonPropertyName("function")]
    public FunctionCall Function { get; init; } = new();

    [JsonPropertyName("extra_content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? ExtraContent { get; init; }
}
