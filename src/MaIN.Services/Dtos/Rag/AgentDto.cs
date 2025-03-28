using System.Text.Json.Serialization;

namespace MaIN.Services.Dtos.Rag;

public class AgentDto
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; init; } = null!;

    [JsonPropertyName("model")] public string Model { get; init; } = null!;
    [JsonPropertyName("description")]
    public string? Description { get; init; }
    [JsonPropertyName("started")]
    public bool Started { get; init; }
    [JsonPropertyName("context")]
    public AgentContextDto Context { get; init; } = null!;

    [JsonPropertyName("order")]
    public int Order { get; init; }

    [JsonPropertyName("behaviours")] public Dictionary<string, string> Behaviours { get; init; } = [];
    [JsonPropertyName("currentBehaviour")] public string CurrentBehaviour { get; init; } = null!;
    [JsonPropertyName("flow")]  public bool Flow { get; init; }
}