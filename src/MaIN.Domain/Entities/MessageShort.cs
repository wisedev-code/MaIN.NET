namespace MaIN.Domain.Entities;

public class MessageShort
{
    public required string Role { get; init; }
    public required string Content { get; init; }
    public DateTime Time { get; set; }
}