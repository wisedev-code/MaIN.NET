using MaIN.Domain.Models;

namespace MaIN.Domain.Entities;

public class Message
{
    public required string Role { get; set; }
    public required string Content { get; set; }
    
    public List<LLMTokenValue> Tokens { get; set; } = [];
    public bool Tool { get; init; }
    public DateTime Time { get; set; }
    public byte[]? Image { get; init; }
    public List<FileInfo>? Files { get; set; } 
    public Dictionary<string, string> Properties { get; set; } = [];
}