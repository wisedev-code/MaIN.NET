using System.Text.Json.Serialization;

namespace MaIN.Services.Models;

public class FileInfoDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("extension")]
    public string Extension { get; set; }
    [JsonPropertyName("content")]
    public string Content { get; set; }
}
