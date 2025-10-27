using System.Text.Json.Serialization;

namespace MaIN.Services.Dtos;

public class FileInfoDto
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = null!;

    [JsonPropertyName("extension")]
    public string Extension { get; init; } = null!;

    [JsonPropertyName("content")]
    public string? Content { get; init; }

    public Stream? StreamContent { get; set; }
}
