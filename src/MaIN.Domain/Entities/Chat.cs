namespace MaIN.Domain.Entities;

public class Chat
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Model { get; set; }
    public List<Message>? Messages { get; set; } = [];
    public ChatType Type { get; set; } = ChatType.Conversation;
    public bool Visual { get; set; } = false;
    public bool Stream { get; set; } = false;
    public Dictionary<string, string> Properties { get; set; } = [];
    public List<string> Memory { get; set; } = [];
}