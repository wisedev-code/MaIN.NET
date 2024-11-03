namespace MaIN.Infrastructure.Models;

public class MessageDocument
{
    public string Role { get; set; }
    public string Content { get; set; }
    public DateTime Time { get; set; }
    public string[] Images { get; set; }
    public string[] Files { get; set; }
    public bool Tool { get; set; }
    public Dictionary<string, string> Properties { get; set; }
}