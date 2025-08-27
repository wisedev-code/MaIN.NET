using MaIN.Domain.Models;

namespace MaIN.Domain.Entities;

public class Message
{
    public const string UnprocessedMessageProperty = "UnprocessedMessage";
    public Message()
    {
        if (Type == MessageType.LocalLLM)
        {
            Properties.Add(UnprocessedMessageProperty, string.Empty);
        }
    }
    
    public required string Role { get; set; }
    public required string Content { get; set; }
    public required MessageType Type { get; set; }
    public List<LLMTokenValue> Tokens { get; set; } = [];
    public bool Tool { get; init; }
    public DateTime Time { get; set; }
    public byte[]? Image { get; init; }
    public byte[]? Speech { get; set; }
    public List<FileInfo>? Files { get; set; } 
    public Dictionary<string, string> Properties { get; set; } = [];

    public Message MarkProcessed()
    {
        Properties.Remove(UnprocessedMessageProperty);
        return this;
    }
}