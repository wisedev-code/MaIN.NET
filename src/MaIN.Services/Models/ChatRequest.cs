using System.Text.Json.Serialization;

namespace MaIN.Services.Models.Ollama;

public class ChatRequest
{
    [JsonPropertyName("model")] 
    public string Model { get; set; }
    [JsonPropertyName("messages")] 
    public List<MessageDto> Messages { get; set; }
    [JsonPropertyName("stream")] 
    public bool Stream { get; set; } = false;
}