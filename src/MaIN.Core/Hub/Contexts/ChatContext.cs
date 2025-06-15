using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using MaIN.Services;
using MaIN.Services.Constants;
using MaIN.Services.Dtos;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;
using FileInfo = MaIN.Domain.Entities.FileInfo;

namespace MaIN.Core.Hub.Contexts;

public class ChatContext
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

    public ChatContext WithModel(string model)
    {
        _chat.Model = model;
        return this;
    }
    
    public ChatContext WithInferenceParams(InferenceParams inferenceParams)
    {
        _chat.InterferenceParams = inferenceParams;
        return this;
    }

    public ChatContext WithMemoryParams(MemoryParams memoryParams)
    {
        _chat.MemoryParams = memoryParams;
        return this;
    }

    public ChatContext WithCustomModel(string model, string path, string? mmProject = null)
    {
        KnownModels.AddModel(model, path, mmProject);
        _chat.Model = model;
        return this;
    }

    public ChatContext WithBackend(BackendType backendType)
    {
        _chat.Backend = backendType;
        return this;
    }

    public ChatContext WithMessage(string content)
    {
        var message = new Message
        {
            Role = "User",
            Content = content,
            Time = DateTime.Now
        };
        
        _chat.Messages.Add(message);
        return this;
    }
    
    
    public ChatContext WithMessage(string content, byte[] image)
    {
        var message = new Message
        {
            Role = "User",
            Content = content,
            Time = DateTime.Now,
            Image = image
        };
        
        _chat.Messages.Add(message);
        return this;
    }

    public ChatContext WithSystemPrompt(string systemPrompt)
    {
        var message = new Message
        {
            Role = "System",
            Content = systemPrompt,
            Time = DateTime.Now
        };
        
        // Insert system message at the beginning
        _chat.Messages.Insert(0, message);
        return this;
    }

    public ChatContext WithFiles(List<FileStream> fileStreams, bool preProcess = false)
    {
        var files = fileStreams.Select(p => new FileInfo()
        {
            Name = Path.GetFileName(p.Name),
            Path = null,
            Extension = Path.GetExtension(p.Name),
            StreamContent = p 
        }).ToList();

        _preProcess = preProcess;
        _files = files;
        return this;
    }

    public ChatContext WithFiles(List<FileInfo> files, bool preProcess = false)
    {
        _files = files;
        _preProcess = preProcess;
        return this;
    }
    
    public ChatContext WithFiles(List<string> filePaths, bool preProcess = false)
    {
        var files = filePaths.Select(p => new FileInfo()
        {
            Name = Path.GetFileName(p),
            Path = p,
            Extension = Path.GetExtension(p)
        }).ToList();

        _preProcess = preProcess;
        _files = files;
        return this;
    }

    public ChatContext EnableVisual()
    {
        _chat.Visual = true;
        return this;
    }

    public ChatContext DisableCache()
    {
        _chat.Properties.AddProperty(ServiceConstants.Properties.DisableCacheProperty);
        return this;
    }
    
    public string GetChatId() => _chat.Id;

    public async Task<ChatResult> CompleteAsync(
        bool translate = false,
        bool interactive = false,
        Func<LLMTokenValue?, Task>? changeOfValue = null)
    {
        if (_chat.Messages.Count == 0)
        {
            throw new InvalidOperationException("Chat has no messages."); //TODO good candidate for domain exception
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
    

    public async Task<Chat> GetCurrentChat()
    {
        if (_chat.Id == null)
            throw new InvalidOperationException("Chat has not been created yet. Call CompleteAsync first.");
            
        return await _chatService.GetById(_chat.Id);
    }

    public async Task<List<Chat>> GetAllChats()
    {
        return await _chatService.GetAll();
    }

    public async Task DeleteChat()
    {
        if (_chat.Id == null)
            throw new InvalidOperationException("Chat has not been created yet.");
            
        await _chatService.Delete(_chat.Id);
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

    // Static methods to create builder from existing chat
    public async Task<ChatContext> FromExisting(string chatId)
    {
        var existingChat = await _chatService.GetById(chatId);
        if (existingChat == null)
        {
            throw new Exception("Chat not found");
        }
        return new ChatContext(_chatService, existingChat);
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