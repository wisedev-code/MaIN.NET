using MaIN.Domain.Entities;
using MaIN.Infrastructure.Repositories.Abstract;
using MaIN.Services.Mappers;
using MaIN.Services.Models.Ollama;
using MaIN.Services.Services.Abstract;

namespace MaIN.Services.Services;

public class ChatService(
    ITranslatorService translatorService,
    IChatRepository chatProvider,
    ILLMService llmService,
    IImageGenService imageGenService) : IChatService
{
    public async Task Create(Chat? chat)
    {
        chat.Type = ChatType.Conversation;
        await chatProvider.AddChat(chat.ToDocument());
    }

    public async Task<ChatResult> Completions(Chat? chat, bool translate = false, bool interactiveUpdates = false)
    {
        if (chat?.Model == ImageGenService.Models.FLUX)
        {
            chat.Visual = true;
        }
        
        var newMsg = chat.Messages.Last();
        newMsg.Time = DateTime.Now;
        var lng = await translatorService.DetectLanguage(newMsg.Content);
        // if (newMsg.Files is not null && newMsg.Files.Count > 0) //Perhaps not needed anymore? TODO
        // {
        //     chat.Messages.AddRange(newMsg.Files.Select(
        //         (file) => new Message()
        //         {
        //             Role = "User",
        //             Tool = true,
        //             Content = $"This is content of attached file. You can see its name and extension, by that you also should be able guess its purpose. You should know its content and provide answers to users questions. Attached File {file.Name} with extension: {file.Extension} and content: {file.Content}"
        //         }));
        // }
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

        var result = chat.Visual 
            ? await imageGenService.Send(chat) 
            : await llmService.Send(chat, interactiveUpdates, true);
    
        if (translate)
        {
            result!.Message.Content = (await translatorService.Translate(result.Message.Content, lng));
            result!.Message.Time = DateTime.Now;
        }
        
        originalMessages.Add(new Message()
        {
            Content = result!.Message.Content,
            Role = result.Message.Role,
            Images = result.Message.Images,
            Time = result.Message.Time
        });
        chat.Messages = originalMessages;

        await chatProvider.UpdateChat(chat.Id, chat.ToDocument());
        return result;
    }

    public async Task Delete(string id)
    {
        await llmService.CleanSessionCache(id);
        await chatProvider.DeleteChat(id);
    }
    
    public async Task<Chat> GetById(string id)
    {
        var chatDocument = await chatProvider.GetChatById(id);
        return chatDocument.ToDomain();
    }

    public async Task<List<Chat>> GetAll()
        => (await chatProvider.GetAllChats())
            .Select(x => x.ToDomain()).ToList();
}