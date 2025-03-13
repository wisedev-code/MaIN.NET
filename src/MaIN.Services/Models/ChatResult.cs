using System.Text.Json.Serialization;

namespace MaIN.Services.Models;

public class ChatResult
{
    [JsonPropertyName("model")] 
    public required string Model { get; init; }

    [JsonPropertyName("created_at")] 
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("message")] 
    public required MessageDto Message { get; init; }

    [JsonPropertyName("done")] 
    public bool Done { get; init; }
}