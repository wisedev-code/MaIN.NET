using LLama.Batched;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities.Tools;
using MaIN.Domain.Models.Abstract;

namespace MaIN.Domain.Entities;

public class Chat
{
    public string Id { get; init; } = string.Empty;
    public required string Name { get; init; }
    public required string ModelId { get; set; }
    public AIModel? ModelInstance { get; set; }
    public List<Message> Messages { get; set; } = [];
    public ChatType Type { get; set; } = ChatType.Conversation;
    public bool Visual { get; set; }
    public InferenceParams InterferenceParams { get; set; } = new();
    public MemoryParams MemoryParams { get; set; } = new();
    public ToolsConfiguration? ToolsConfiguration { get; set; }
    public TextToSpeechParams? TextToSpeechParams { get; set; }
    public Dictionary<string, string> Properties { get; init; } = [];
    public List<string> Memory { get; } = [];
    public BackendType? Backend { get; set; } // TODO: remove because of ModelInstance
    public Conversation.State? ConversationState { get; set; }

    public bool Interactive = false;
    public bool Translate = false;
    
}