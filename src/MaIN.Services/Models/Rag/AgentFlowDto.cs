using System.Text.Json.Serialization;

namespace MaIN.Services.Models.Rag;

public class AgentFlowDto
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    [JsonPropertyName("description")]
    public string? Description { get; init; }
    [JsonPropertyName("agents")] public List<AgentDto> Agents { get; init; } = [];
}