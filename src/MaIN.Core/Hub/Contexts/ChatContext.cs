using MaIN.Domain.Entities;
using MaIN.Services.Models.Ollama;
using MaIN.Services.Services.Abstract;
using FileInfo = MaIN.Domain.Entities.FileInfo;

namespace MaIN.Core.Hub.Contexts;

public class ChatContext
{
    private readonly IChatService _chatService;
    private Chat _chat;

    internal ChatContext(IChatService chatService)
    {
        _chatService = chatService;
        _chat = new Chat
        {
            Id = Guid.NewGuid().ToString(),
            Messages = new List<Message>()
        };
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

    public ChatContext WithContent(string content)
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

    public ChatContext WithFiles(List<FileInfo> files)
    {
        var lastMessage = _chat.Messages.LastOrDefault();
        if (lastMessage != null)
        {
            lastMessage.Files = files;
        }
        return this;
    }

    public ChatContext EnableVisual()
    {
        _chat.Visual = true;
        return this;
    }

    public async Task<ChatResult> CompleteAsync(bool translate = false, bool interactive = false)
    {
        if (_chat.Id == null || !await ChatExists(_chat.Id))
        {
            await _chatService.Create(_chat);
        }
        return await _chatService.Completions(_chat, translate, interactive);
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
    public static async Task<ChatContext> FromExisting(IChatService chatService, string chatId)
    {
        var existingChat = await chatService.GetById(chatId);
        return new ChatContext(chatService, existingChat);
    }
}