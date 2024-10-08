using System.Text.Json.Serialization;

namespace MaIN.Models.Rag;

public class AgentDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("model")]
    public string Model { get; set; }
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    [JsonPropertyName("started")]
    public bool Started { get; set; }
    [JsonPropertyName("context")]
    public AgentContextDto Context { get; set; }
}