using System.Text.Json.Serialization;

namespace MaIN.Services.Models;

public class ChatOllamaResult
{
    [JsonPropertyName("model")] 
    public string Model { get; set; }

    [JsonPropertyName("created_at")] 
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("message")] 
    public MessageDto Message { get; set; }

    [JsonPropertyName("done")] 
    public bool Done { get; set; }
}