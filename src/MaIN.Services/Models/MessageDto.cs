using System.Text.Json.Serialization;

namespace MaIN.Services.Models;

public class MessageDto
{
    [JsonPropertyName("role")] 
    public string Role { get; set; }

    [JsonPropertyName("content")] 
    public string Content { get; set; }
    [JsonPropertyName("time")]
    public DateTime Time { get; set; }
    [JsonPropertyName("images")] 
    public string[] Images { get; set; }
    [JsonPropertyName("files")] 
    public FileInfoDto[]? Files { get; set; }
    [JsonPropertyName("properties")]
    public Dictionary<string, string> Properties { get; set; }
}