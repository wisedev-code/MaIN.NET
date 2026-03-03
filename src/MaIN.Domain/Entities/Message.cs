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
    public List<byte[]>? Images { get; set; }

    // Backward-compat wrapper – single image access
    public byte[]? Image
    {
        get => Images?.Count > 0 ? Images[0] : null;
        set
        {
            if (value == null) Images = null;
            else Images = [value];
        }
    }

    public byte[]? Speech { get; set; }
    public List<FileInfo>? Files { get; set; } 
    public Dictionary<string, string> Properties { get; set; } = [];

    public Message MarkProcessed()
    {
        Properties.Remove(UnprocessedMessageProperty);
        return this;
    }
}