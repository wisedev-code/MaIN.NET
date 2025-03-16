using System.Text.Json.Serialization;

namespace MaIN.Services.Models;

public class ChatDto
{
    [JsonPropertyName("id")] 
    public string? Id { get; set; }
    [JsonPropertyName("name")] 
    public string? Name { get; init; }
    [JsonPropertyName("model")] 
    public string? Model { get; init; }
    [JsonPropertyName("messages")] 
    public List<MessageDto>? Messages { get; init; }
    [JsonPropertyName("type")] 
    public ChatTypeDto Type { get; init; }

    [JsonPropertyName("stream")] 
    public bool Stream { get; set; } = false;

    [JsonPropertyName("properties")] public Dictionary<string, string> Properties { get; init; } = [];

    [JsonPropertyName("visual")]
    public bool Visual { get; set; }
}