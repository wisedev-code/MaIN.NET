using MaIN.Core.Hub.Contexts.Interfaces.ChatContext;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Tools;
using MaIN.Domain.Exceptions.Chats;
using MaIN.Domain.Models;
using MaIN.Services;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;
using FileInfo = MaIN.Domain.Entities.FileInfo;

namespace MaIN.Core.Hub.Contexts;

public sealed class ChatContext : IChatBuilderEntryPoint, IChatMessageBuilder, IChatCompletionBuilder
{
    private readonly IChatService _chatService;
    private bool _preProcess;
    private Chat _chat { get; set; }
    private List<FileInfo> _files { get; set; } = [];

    internal ChatContext(IChatService chatService)
    {
        _chatService = chatService;
        _chat = new Chat
        {
            Name = "New Chat",
            Id = Guid.NewGuid().ToString(),
            Messages = new List<Message>(),
            Model = string.Empty
        };
        _files = [];
    }

    internal ChatContext(IChatService chatService, Chat existingChat)
    {
        _chatService = chatService;
        _chat = existingChat;
    }


    public IChatMessageBuilder WithModel(string model)
    {
        _chat.Model = model;
        return this;
    }

    public IChatMessageBuilder WithCustomModel(string model, string path, string? mmProject = null)
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
    
    public IChatCompletionBuilder WithInferenceParams(InferenceParams inferenceParams)
    {
        _chat.InterferenceParams = inferenceParams;
        return this;
    }

    public IChatCompletionBuilder WithTools(ToolsConfiguration toolsConfiguration)
    {
        _chat.ToolsConfiguration = toolsConfiguration;
        return this;
    }

    public IChatCompletionBuilder WithMemoryParams(MemoryParams memoryParams)
    {
        _chat.MemoryParams = memoryParams;
        return this;
    }

    public IChatCompletionBuilder Speak(TextToSpeechParams speechParams)
    {
        _chat.Visual = false;
        _chat.TextToSpeechParams = speechParams;
        return this;
    }

    public IChatCompletionBuilder WithBackend(BackendType backendType)
    {
        _chat.Backend = backendType;
        return this;
    }

    public IChatCompletionBuilder WithSystemPrompt(string systemPrompt)
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

    public IChatCompletionBuilder WithMessage(string content)
    {
        _chat.Messages.Add(new Message { Role = "User", Content = content, Type = MessageType.LocalLLM, Time = DateTime.Now });
        return this;
    }

    public IChatCompletionBuilder WithMessage(string content, byte[] image)
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

    public IChatCompletionBuilder WithMessages(IEnumerable<Message> messages)
    {
        _chat.Messages.AddRange(messages);
        return this;
    }

    public IChatCompletionBuilder WithFiles(List<FileStream> file, bool preProcess = false)
    {
        _files = file.Select(f => new FileInfo { Name = Path.GetFileName(f.Name), StreamContent = f, Extension = Path.GetExtension(f.Name) })
            .ToList();
        _preProcess = preProcess;
        return this;
    }

    public IChatCompletionBuilder WithFiles(List<FileInfo> file, bool preProcess = false)
    {
        _files = file;
        _preProcess = preProcess;
        return this;
    }

    public IChatCompletionBuilder WithFiles(List<string> file, bool preProcess = false)
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

    public IChatCompletionBuilder DisableCache()
    {
        _chat.Properties.AddProperty(ServiceConstants.Properties.DisableCacheProperty);
        return this;
    }
    
    public async Task<ChatResult> CompleteAsync(
        bool translate = false,
        bool interactive = false,
        Func<LLMTokenValue?, Task>? changeOfValue = null)
    {
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

    public async Task<IChatCompletionBuilder> FromExisting(string chatId)
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