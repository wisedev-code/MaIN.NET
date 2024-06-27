using System.Text.Json;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Providers.cs.Abstract;
using MaIN.Models;
using MaIN.Services.Mappers;
using MaIN.Services.Models;
using MaIN.Services.Models.Ollama;
using MaIN.Services.Services.Abstract;

namespace MaIN.Services.Services;

public class ChatService(
    ITranslatorService translatorService,
    IChatProvider chatProvider,
    IHttpClientFactory httpClientFactory) : IChatService
{
    public async Task Create(Chat chat)
        => await chatProvider.AddChat(chat.ToDocument());

    public async Task<ChatOllamaResult> Completions(Chat chat)
    {
        var lng = await translatorService.DetectLanguage(chat.Messages.Last().Content);
        var originalMessages = chat.Messages;
        var translatedMessages = await Task.WhenAll(chat.Messages.Select(async m => new Message()
        {
            Role = m.Role,
            Content = await translatorService.Translate(m.Content, "en")
        }));
        chat.Messages = translatedMessages.ToList();

        using var client = httpClientFactory.CreateClient();
        var response = await client.PostAsync($"{GetLocalhost()}:11434/api/chat",
            new StringContent(JsonSerializer.Serialize(new ChatOllama()
            {
                Messages = chat.Messages.Select(x => new MessageDto()
                {
                    Content = x.Content,
                    Role = x.Role
                }).ToList(),
                Model = chat.Model,
                Stream = chat.Stream
            }), System.Text.Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to create completion for chat {chat.Id} with message " +
                                $"{chat.Messages.Last().Content}, status code {response.StatusCode}");
        }

        // Read the response from Ollama
        var responseBody = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ChatOllamaResult>(responseBody);
        result!.Message.Content = (await translatorService.Translate(result.Message.Content, lng));

        originalMessages.Add(new Message()
        {
            Content = result.Message.Content,
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

    public async Task<List<string>> GetCurrentModels()
    {
        using var client = httpClientFactory.CreateClient();
        var response = await client.GetAsync($"{GetLocalhost()}:11434/api/tags");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to fetch models from Ollama, status code {response.StatusCode}");
        }
        
        var result = JsonSerializer.Deserialize<ModelsOllamaResponse>(
            await response.Content.ReadAsStringAsync());

        return result!.Models.Select(x => x.Name).ToList();
    }

    public async Task<List<Chat>> GetAll()
        => (await chatProvider.GetAllChats())
            .Select(x => x.ToDomain()).ToList();

    private static string GetLocalhost() =>
        Environment.GetEnvironmentVariable("LocalHost") ?? "http://localhost";
}