using System.Text.Json.Serialization;

namespace MainFE.Components.Models;

public class ChatResponse
{
    [JsonPropertyName("model")] public string Model { get; set; }
    [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }
    [JsonPropertyName("message")] public Message Message { get; set; }
    [JsonPropertyName("done")] public bool Done { get; set; }
}