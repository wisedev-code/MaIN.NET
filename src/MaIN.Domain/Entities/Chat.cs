namespace MaIN.Domain.Entities;

public class Chat
{
    public string Id { get; init; } = null!;
    public required string Name { get; init; }
    public required string Model { get; set; }
    public List<Message> Messages { get; set; } = [];
    public ChatType Type { get; set; } = ChatType.Conversation;
    public bool Visual { get; set; }
    public InferenceParams InterferenceParams { get; set; } = new();
    public Dictionary<string, string> Properties { get; init; } = [];
    public List<string> Memory { get; } = [];

    public bool Interactive = false;
    public bool Translate = false;
}