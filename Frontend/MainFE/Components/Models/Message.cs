namespace MainFE.Components.Models;

public class Message
{
    public string? Content { get; set; }
    public string Role { get; set; }
    public string[]? Images { get; set; }
    public FileData[]? Files { get; set; }
}