using System.Text.Json.Serialization;
using MaIN.Services.Dtos.Rag.AgentSource;

namespace MaIN.Services.Dtos.Rag;

public class AgentContextDto
{
    [JsonPropertyName("instruction")]
    public string Instruction { get; init; } = null!;

    [JsonPropertyName("source")]
    public AgentSourceDto Source { get; init; } = null!;

    [JsonPropertyName("steps")]
    public List<string> Steps { get; init; } = null!;

    [JsonPropertyName("relations")]
    public List<string>? Relations { get; init; }

}