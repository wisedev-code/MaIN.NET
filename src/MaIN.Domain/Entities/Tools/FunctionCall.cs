using System.Text.Json.Serialization;

namespace MaIN.Domain.Entities.Tools;

public sealed record FunctionCall
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string Arguments { get; init; } = "{}";
}
