using System.Text.Json.Serialization;

namespace MaIN.Models;

public class Message
{
    public string Role { get; set; }
    public string Content { get; set; }
}