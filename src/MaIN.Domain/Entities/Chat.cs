using LLama.Batched;
using MaIN.Domain.Configuration;
using MaIN.Domain.Configuration.BackendInferenceParams;
using MaIN.Domain.Entities.Tools;
using MaIN.Domain.Models.Abstract;
using Grammar = MaIN.Domain.Models.Grammar;

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
    public IBackendInferenceParams BackendParams { get; set; } = new LocalInferenceParams();
    public LocalInferenceParams? LocalParams => BackendParams as LocalInferenceParams;

    public Grammar? InferenceGrammar
    {
        get => BackendParams switch
        {
            LocalInferenceParams p => p.Grammar,
            OpenAiInferenceParams p => p.Grammar,
            DeepSeekInferenceParams p => p.Grammar,
            GroqCloudInferenceParams p => p.Grammar,
            XaiInferenceParams p => p.Grammar,
            GeminiInferenceParams p => p.Grammar,
            AnthropicInferenceParams p => p.Grammar,
            OllamaInferenceParams p => p.Grammar,
            _ => null
        };
        set
        {
            switch (BackendParams)
            {
                case LocalInferenceParams p: p.Grammar = value; break;
                case OpenAiInferenceParams p: p.Grammar = value; break;
                case DeepSeekInferenceParams p: p.Grammar = value; break;
                case GroqCloudInferenceParams p: p.Grammar = value; break;
                case XaiInferenceParams p: p.Grammar = value; break;
                case GeminiInferenceParams p: p.Grammar = value; break;
                case AnthropicInferenceParams p: p.Grammar = value; break;
                case OllamaInferenceParams p: p.Grammar = value; break;
            }
        }
    }
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