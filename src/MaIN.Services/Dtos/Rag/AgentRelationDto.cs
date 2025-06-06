using System.Text.Json.Serialization;

namespace MaIN.Services.Dtos.Rag;

public class AgentRelationDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    [JsonPropertyName("agentPurpose")]
    public string? AgentPurpose { get; set; }
}