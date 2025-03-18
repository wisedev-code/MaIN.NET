using System.Text.Json.Serialization;

namespace MaIN.Services.Models;

public class FileInfoDto
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }
    [JsonPropertyName("extension")]
    public string? Extension { get; init; }
    [JsonPropertyName("content")]
    public string? Content { get; init; }

    public FileStream? StreamContent { get; set; }
}
