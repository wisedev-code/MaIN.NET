namespace MaIN.Domain.Entities;

public class Chat
{
    public string Id { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string Model { get; set; } = null!;
    public List<Message> Messages { get; set; } = [];
    public ChatType Type { get; set; } = ChatType.Conversation;
    public bool Visual { get; set; } = false;
    public InferenceParams InterferenceParams { get; set; } = new();
    public Dictionary<string, string> Properties { get; init; } = [];
    public List<string> Memory { get; } = [];

    public bool Interactive = false;
    public bool Translate = false;
}