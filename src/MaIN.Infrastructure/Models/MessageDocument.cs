using MaIN.Domain.Models;

namespace MaIN.Infrastructure.Models;

public class MessageDocument
{
    public required string Role { get; init; }
    public required string Content { get; init; }
    public required string MessageType { get; init; }
    public DateTime Time { get; init; }
    public byte[]? Images { get; init; }
    public string[]? Files { get; set; }
    public bool Tool { get; init; }
    public Dictionary<string, string> Properties { get; init; } = [];
    public List<LLMTokenValueDocument> Tokens { get; set; } = [];
}