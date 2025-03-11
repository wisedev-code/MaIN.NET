namespace MaIN.Domain.Entities;

public class Message
{
    public required string Role { get; set; }
    public required string Content { get; set; }
    public bool Tool { get; init; }
    public DateTime Time { get; set; }
    public byte[]? Images { get; init; }
    public List<FileInfo>? Files { get; set; } //Temporary solution
    public Dictionary<string, string>? Properties { get; set; } = [];
}