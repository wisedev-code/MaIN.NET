using System.Text.Json.Serialization;

namespace MaIN.Models;

public class Chat
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Model { get; set; }
    public List<Message> Messages { get; set; }
    public bool Stream { get; set; } = false;
}