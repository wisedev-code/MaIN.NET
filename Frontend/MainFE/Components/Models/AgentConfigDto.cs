using System.Text.Json.Serialization;

namespace MaIN.Models.Rag;

public class AgentContextDto
{
    [JsonPropertyName("instruction")]
    public string Instruction { get; set; }
    [JsonPropertyName("source")]
    public object Source { get; set; }
    [JsonPropertyName("steps")]
    public List<string> Steps { get; set; }
    [JsonPropertyName("relations")]
    public List<string>? Relations { get; set; }

}