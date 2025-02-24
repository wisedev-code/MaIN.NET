using System.Text.Json.Serialization;
using MaIN.Services.Models.Rag.AgentSource;

namespace MaIN.Services.Models.Rag;

public class AgentContextDto
{
    [JsonPropertyName("instruction")]
    public string Instruction { get; set; }
    [JsonPropertyName("source")]
    public AgentSourceDto Source { get; set; }
    [JsonPropertyName("steps")]
    public List<string> Steps { get; set; }
    [JsonPropertyName("relations")]
    public List<string>? Relations { get; set; }

}