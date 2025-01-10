using MaIN.Domain.Entities;
using MaIN.Services.Models.Ollama;
using MaIN.Services.Services.Abstract;

namespace MaIN.Core.Hub;

public class ChatBuilder
{
    private readonly IChatService _chatService;
    private Chat _chat = new();

    internal ChatBuilder(IChatService chatService)
    {
        _chatService = chatService;
    }

    public ChatBuilder WithContent(string content)
    {
        //_chat.Content = content;
        return this;
    }

    public ChatBuilder WithSystemPrompt(string systemPrompt)
    {
        //_chat.SystemPrompt = systemPrompt;
        return this;
    }

    public async Task<ChatResult> CompleteAsync(bool translate = false, bool interactive = false)
    {
        await _chatService.Create(_chat);
        return await _chatService.Completions(_chat, translate, interactive);
    }
}