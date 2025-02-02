using System.Text.Json.Serialization;

namespace MaIN.Services.Models;

public class ChatRequest
{
    [JsonPropertyName("model")] 
    public string Model { get; set; }
    [JsonPropertyName("messages")] 
    public List<MessageDto> Messages { get; set; }
    [JsonPropertyName("stream")] 
    public bool Stream { get; set; } = false;
}