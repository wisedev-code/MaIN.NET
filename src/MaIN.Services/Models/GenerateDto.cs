using System.Text.Json.Serialization;

namespace MaIN.Services.Models;

public class GenerateDto
{
    [JsonPropertyName("model")] public string Model { get; set; }

    [JsonPropertyName("prompt")] public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("stream")] public bool Stream { get; set; } = false;
}