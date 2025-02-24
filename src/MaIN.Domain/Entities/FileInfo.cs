namespace MaIN.Domain.Entities;

public class FileInfo
{
    public required string Name { get; set; }
    public required string Extension { get; set; }
    public string? Content { get; set; }
    public string? Path { get; set; }
}