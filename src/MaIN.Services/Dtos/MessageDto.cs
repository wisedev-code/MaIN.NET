using System.Text.Json.Serialization;

namespace MaIN.Services.Dtos;

public class MessageDto
{
    [JsonPropertyName("role")] 
    public string Role { get; init; } = null!;

    [JsonPropertyName("content")] 
    public string Content { get; set; } = null!;
    
    [JsonPropertyName("type")] 
    public string Type { get; set; } = null!;
    [JsonPropertyName("time")]
    public DateTime Time { get; set; }
    [JsonPropertyName("images")] 
    public byte[]? Images { get; init; }
    [JsonPropertyName("files")] 
    public FileInfoDto[]? Files { get; init; }
    [JsonPropertyName("properties")]
    public Dictionary<string, string> Properties { get; set; } = [];
}