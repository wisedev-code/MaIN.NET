namespace MaIN.Domain.Entities;

public class Message
{
    public string Role { get; set; }
    public string Content { get; set; }
    public bool Tool { get; set; }
    public DateTime Time { get; set; }
    public string[] Images { get; set; }
    public List<FileInfo>? Files { get; set; } //Temporary solution
}