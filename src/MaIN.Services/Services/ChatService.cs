using System.Text.Json;
using MaIN.Domain.Entities;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;
using MaIN.Models;
using MaIN.Services.Mappers;
using MaIN.Services.Models;
using MaIN.Services.Models.Ollama;
using MaIN.Services.Services.Abstract;

namespace MaIN.Services.Services;

public class ChatService(
    ITranslatorService translatorService,
    IChatRepository chatProvider,
    IOllamaService ollamaService,
    IHttpClientFactory httpClientFactory) : IChatService
{
    public async Task Create(Chat chat)
        => await chatProvider.AddChat(chat.ToDocument());

    public async Task<ChatOllamaResult> Completions(Chat chat, bool translate = false)
    {
        var lng = await translatorService.DetectLanguage(chat.Messages.Last().Content);
        var originalMessages = chat.Messages;

        if (translate)
        {
            var translatedMessages = await Task.WhenAll(chat.Messages.Select(async m => new Message()
            {
                Role = m.Role,
                Content = await translatorService.Translate(m.Content, "en")
            }));
            chat.Messages = translatedMessages.ToList();
        }

        var result = await ollamaService.Send(chat);
    
        if (translate)
        {
            result!.Message.Content = (await translatorService.Translate(result.Message.Content, lng));
        }
        
        originalMessages.Add(new Message()
        {
            Content = result!.Message.Content,
            Role = result.Message.Role
        });
        chat.Messages = originalMessages;

        await chatProvider.UpdateChat(chat.Id, chat.ToDocument());

        return result;
    }

    public async Task Delete(string id)
        => await chatProvider.DeleteChat(id);

    public async Task<Chat> GetById(string id)
    {
        var chatDocument = await chatProvider.GetChatById(id);
        return chatDocument.ToDomain();
    }

    public async Task<List<Chat>> GetAll()
        => (await chatProvider.GetAllChats())
            .Select(x => x.ToDomain()).ToList();

   
}