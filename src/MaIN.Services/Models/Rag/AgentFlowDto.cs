using System.Text.Json.Serialization;

namespace MaIN.Services.Models.Rag;

public class AgentFlowDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("description")]
    public string Description { get; set; }
    [JsonPropertyName("agents")]
    public List<AgentDto> Agents { get; set; }
}