using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Exceptions;
using MaIN.Domain.Models;
using MaIN.Infrastructure.Repositories.Abstract;
using MaIN.Services.Mappers;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.ImageGenServices;
using MaIN.Services.Services.LLMService;
using MaIN.Services.Services.LLMService.Factory;
using MaIN.Services.Services.Models;

namespace MaIN.Services.Services;

public class ChatService(
    ITranslatorService translatorService,
    IChatRepository chatProvider,
    ILLMServiceFactory llmServiceFactory,
    IImageGenServiceFactory imageGenServiceFactory,
    MaINSettings settings) : IChatService
{
    public async Task Create(Chat chat)
    {
        chat.Type = ChatType.Conversation;
        await chatProvider.AddChat(chat.ToDocument());
    }

  public async Task<ChatResult> Completions(
    Chat chat,
    bool translate = false,
    bool interactiveUpdates = false,
    Func<LLMTokenValue?, Task>? changeOfValue = null)
{
    if (chat.Model == ImageGenService.LocalImageModels.FLUX) 
    {
        chat.Visual = true;
    }
    chat.Backend ??= settings.BackendType;
    
    chat.Messages.Where(x => x.Type == MessageType.NotSet).ToList()
        .ForEach(x => x.Type = chat.Backend != BackendType.Self ? MessageType.CloudLLM : MessageType.LocalLLM);
    
    translate = translate || chat.Translate;
    interactiveUpdates = interactiveUpdates || chat.Interactive;
    var newMsg = chat.Messages.Last();
    newMsg.Time = DateTime.Now;
    
    var lng = translate ? await translatorService.DetectLanguage(newMsg.Content) : null;
    var originalMessages = chat.Messages;
    
    if (translate)
    {
        chat.Messages = (await Task.WhenAll(chat.Messages.Select(async m => new Message()
        {
            Role = m.Role,
            Content = await translatorService.Translate(m.Content, "en"),
            Type = m.Type
        }))).ToList();
    }

    var result = chat.Visual 
        ? await imageGenServiceFactory.CreateService(chat.Backend.Value).Send(chat) 
        : await llmServiceFactory.CreateService(chat.Backend.Value).Send(chat, new ChatRequestOptions()
        {
            InteractiveUpdates = interactiveUpdates,
            TokenCallback = changeOfValue
        });

    if (translate)
    {
        result!.Message.Content = await translatorService.Translate(result.Message.Content, lng!);
        result.Message.Time = DateTime.Now;
    }
    
    originalMessages.Add(result!.Message);
    chat.Messages = originalMessages;
    
    await chatProvider.UpdateChat(chat.Id!, chat.ToDocument());
    return result;
}

    public async Task Delete(string id)
    {
        var chat = await chatProvider.GetChatById(id);
        var llmService = llmServiceFactory.CreateService(chat?.Backend ?? settings.BackendType);
        await llmService.CleanSessionCache(id);
        await chatProvider.DeleteChat(id);
    }
    
    public async Task<Chat> GetById(string id)
    {
        var chatDocument = await chatProvider.GetChatById(id);
        if (chatDocument == null)
        {
            throw new ChatNotFoundException(id);
            // throw new Exception("Chat not found");
            //TODO good candidate for custom exception
        }
        return chatDocument.ToDomain();
    }

    public async Task<List<Chat>> GetAll()
        => (await chatProvider.GetAllChats())
            .Select(x => x.ToDomain()).ToList();
}