namespace MaIN.Domain.Entities;

public class FileInfo
{
    public required string Name { get; set; }
    public required string Extension { get; set; }
    public string? Content { get; set; }
    public Stream? StreamContent { get; set; }
    public string? Path { get; set; }
}