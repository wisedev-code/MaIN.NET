using MaIN.Core.Hub.Contexts.Interfaces.ChatContext;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Tools;
using MaIN.Domain.Exceptions.Chats;
using MaIN.Domain.Models;
using MaIN.Domain.Models.Abstract;
using MaIN.Services;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;
using FileInfo = MaIN.Domain.Entities.FileInfo;

namespace MaIN.Core.Hub.Contexts;

public sealed class ChatContext : IChatBuilderEntryPoint, IChatMessageBuilder, IChatConfigurationBuilder
{
    private readonly IChatService _chatService;
    private bool _preProcess;
    private readonly Chat _chat;
    private List<FileInfo> _files = [];

    internal ChatContext(IChatService chatService)
    {
        _chatService = chatService;
        _chat = new Chat
        {
            Name = "New Chat",
            Id = Guid.NewGuid().ToString(),
            Messages = [],
            ModelId = string.Empty
        };
    }

    internal ChatContext(IChatService chatService, Chat existingChat)
    {
        _chatService = chatService;
        _chat = existingChat;
    }

    public IChatMessageBuilder WithModel(AIModel model)
    {
        _chat.ModelInstance = model;
        _chat.ModelId = model.Id;
        _chat.Backend = model.Backend;
        return this;
    }

    public IChatMessageBuilder WithModel<TModel>() where TModel : AIModel, new()
    {
        var model = new TModel();
        return WithModel(model);
    }

    [Obsolete("Use WithModel(AIModel model) or WithModel<TModel>() instead.")]
    public ChatContext WithModel(string modelId)
    {
        var model = ModelRegistry.GetById(modelId);
        SetModel(model);
        return this;
    }

    private void SetModel(AIModel model)
    {
        _chat.ModelId = model.Id;
        _chat.ModelInstance = model;
        _chat.Backend = model.Backend;
    }

    public IChatMessageBuilder WithInferenceParams(InferenceParams inferenceParams)
    {
        KnownModels.AddModel(model, path, mmProject);
        _chat.Model = model;
        return this;
    }

    public IChatMessageBuilder EnableVisual()
    {
        _chat.Visual = true;
        return this;
    }
    
    public IChatConfigurationBuilder WithInferenceParams(InferenceParams inferenceParams)
    {
        _chat.InterferenceParams = inferenceParams;
        return this;
    }

    [Obsolete("Use WithModel<TModel>() instead.")]
    public ChatContext WithCustomModel(string model, string path, string? mmProject = null)
    {
        KnownModels.AddModel(model, path, mmProject);
        _chat.ModelId = model;
        return this;
    }

    public IChatConfigurationBuilder WithMemoryParams(MemoryParams memoryParams)
    {
        _chat.MemoryParams = memoryParams;
        return this;
    }

    public IChatConfigurationBuilder Speak(TextToSpeechParams speechParams)
    {
        _chat.Visual = false;
        _chat.TextToSpeechParams = speechParams;
        return this;
    }

    public IChatConfigurationBuilder WithBackend(BackendType backendType)
    {
        _chat.Backend = backendType;
        return this;
    }

    public IChatConfigurationBuilder WithSystemPrompt(string systemPrompt)
    {
        var message = new Message
        {
            Role = "System",
            Content = systemPrompt,
            Type = MessageType.NotSet,
            Time = DateTime.Now
        };

        _chat.Messages.Insert(0, message);
        return this;
    }

    public IChatConfigurationBuilder WithMessage(string content)
    {
        _chat.Messages.Add(new Message { Role = "User", Content = content, Type = MessageType.LocalLLM, Time = DateTime.Now });
        return this;
    }

    public IChatConfigurationBuilder WithMessage(string content, byte[] image)
    {
        var message = new Message
        {
            Role = "User",
            Content = content,
            Type = MessageType.NotSet,
            Time = DateTime.Now,
            Image = image
        };

        _chat.Messages.Add(message);
        return this;
    }

    public IChatConfigurationBuilder WithMessages(IEnumerable<Message> messages)
    {
        _chat.Messages.AddRange(messages);
        return this;
    }

    public IChatConfigurationBuilder WithFiles(List<FileStream> file, bool preProcess = false)
    {
        _files = file.Select(f => new FileInfo { Name = Path.GetFileName(f.Name), StreamContent = f, Extension = Path.GetExtension(f.Name) })
            .ToList();
        _preProcess = preProcess;
        return this;
    }

    public IChatConfigurationBuilder WithFiles(List<FileInfo> file, bool preProcess = false)
    {
        _files = file;
        _preProcess = preProcess;
        return this;
    }

    public IChatConfigurationBuilder WithFiles(List<string> file, bool preProcess = false)
    {
        _files = file
            .Select(path =>
                new FileInfo
                {
                    Name = Path.GetFileName(path),
                    Path = path,
                    Extension = Path.GetExtension(path)
                })
            .ToList();
        _preProcess = preProcess;
        return this;
    }

    public IChatConfigurationBuilder DisableCache()
    {
        _chat.Properties.AddProperty(ServiceConstants.Properties.DisableCacheProperty);
        return this;
    }
    
    public async Task<ChatResult> CompleteAsync(
        bool translate = false, // Move to WithTranslate
        bool interactive = false, // Move to WithInteractive
        Func<LLMTokenValue?, Task>? changeOfValue = null)
    {
        if (_chat.ModelInstance is null)
        {
            throw new ChatConfigurationException("Model is required. Use .WithModel() before calling CompleteAsync().");
        }
        if (_chat.Messages.Count == 0)
        {
            throw new EmptyChatException(_chat.Id);
        }
        
        _chat.Messages.Last().Files = _files;
        if(_preProcess)
        {
            _chat.Messages.Last().Properties.AddProperty(ServiceConstants.Properties.PreProcessProperty);
        }
        
        if (!await ChatExists(_chat.Id))
        {
            await _chatService.Create(_chat);
        }
        var result = await _chatService.Completions(_chat, translate, interactive, changeOfValue);
        _files = [];
        return result;
    }

    public async Task<IChatConfigurationBuilder> FromExisting(string chatId)
    {
        var existing = await _chatService.GetById(chatId);
        return existing == null 
            ? throw new ChatNotFoundException(chatId) 
            : new ChatContext(_chatService, existing);
    }

    private async Task<bool> ChatExists(string id)
    {
        try
        {
            await _chatService.GetById(id);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    IChatMessageBuilder IChatMessageBuilder.EnableVisual() => EnableVisual();

 
    public string GetChatId() => _chat.Id;
    
    public async Task<Chat> GetCurrentChat()
    {
        if (_chat.Id == null)
        {
            throw new ChatNotInitializedException();
        }

        return await _chatService.GetById(_chat.Id);
    }

    public async Task<List<Chat>> GetAllChats()
    {
        return await _chatService.GetAll();
    }
    
    public async Task DeleteChat()
    {
        if (_chat.Id == null)
        {
            throw new ChatNotInitializedException();
        }

        await _chatService.Delete(_chat.Id);
    }
    
    public List<MessageShort> GetChatHistory()
    {
        return _chat.Messages.Select(x => new MessageShort()
        {
            Content = x.Content,
            Role = x.Role,
            Time = x.Time
        }).ToList();
    }
}