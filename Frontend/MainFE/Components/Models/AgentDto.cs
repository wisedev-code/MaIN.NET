using System.Text.Json.Serialization;
using MainFE.Components.Models;

namespace MaIN.Models.Rag;

public class AgentDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("model")]
    public string Model { get; set; }
    [JsonPropertyName("order")]
    public int Order { get; set; }
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    [JsonPropertyName("behaviours")]
    public Dictionary<string,string> Behaviours { get; set; }
    [JsonPropertyName("started")]
    public bool Started { get; set; }
    
    [JsonPropertyName("context")]
    public AgentContextDto Context { get; set; }
    public AgentProcessingState State { get; set; }

    public bool IsProcessing { get; set; }
    public List<string>? AgentDependencies { get; set; } = [];
    public string? ProgressMessage { get; set; }
    public string? Behaviour { get; set; } = "Default";
}