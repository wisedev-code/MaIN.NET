namespace MainFE.Components.Models;

public class Message
{
    public string? Content { get; set; }
    public string Role { get; set; }
    public DateTime Time { get; set; }
    public string[]? Images { get; set; }
    public FileData[]? Files { get; set; }
    public Dictionary<string, string>? Properties { get; set; } = [];
    public bool? IsInternal => Properties?.Any(x => x is { Key: "agent_internal", Value: "true" });
}