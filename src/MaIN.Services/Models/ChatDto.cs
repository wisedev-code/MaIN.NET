using System.Text.Json.Serialization;

namespace MaIN.Services.Models;

public class ChatDto
{
    [JsonPropertyName("id")] 
    public string Id { get; set; }
    [JsonPropertyName("name")] 
    public string Name { get; set; }
    [JsonPropertyName("model")] 
    public string Model { get; set; }
    [JsonPropertyName("messages")] 
    public List<MessageDto> Messages { get; set; }

    [JsonPropertyName("stream")] 
    public bool Stream { get; set; } = false;
}