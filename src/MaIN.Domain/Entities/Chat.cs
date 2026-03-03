using LLama.Batched;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities.Tools;
using MaIN.Domain.Models.Abstract;

namespace MaIN.Domain.Entities;

public class Chat
{
    public string Id { get; init; } = string.Empty;
    public required string Name { get; init; }
    private string? _modelId;
    public required string ModelId
    {
        get => _modelInstance?.Id ?? _modelId ?? string.Empty;
        set
        {
            _modelId = value;
            if (string.IsNullOrEmpty(value))
            {
                _modelInstance = null;
                return;
            }

            ModelRegistry.TryGetById(value, out _modelInstance);
        }
    }
    private AIModel? _modelInstance;
    public AIModel? ModelInstance
    {
        get => _modelInstance;
        set
        {
            _modelInstance = value;
            _modelId = value?.Id ?? string.Empty;
        }
    }
    public List<Message> Messages { get; set; } = [];
    public ChatType Type { get; set; } = ChatType.Conversation;
    public bool ImageGen { get; set; }
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