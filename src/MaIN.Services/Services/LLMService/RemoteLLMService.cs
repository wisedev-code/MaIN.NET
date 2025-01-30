using System.Net.Http.Json;
using MaIN.Domain.Entities;
using MaIN.Services.Models.Ollama;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Utils;

namespace MaIN.Services.Services.LLMService;

public class RemoteLLMService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly INotificationService _notificationService;

    public RemoteLLMService(HttpClient httpClient, INotificationService notificationService)
    {
        _httpClient = httpClient;
        _notificationService = notificationService;
    }

    public async Task<ChatResult?> Send(Chat? chat, bool interactiveUpdates = false, bool newSession = false)
    {
        if (chat == null || chat.Messages == null || !chat.Messages.Any())
            return null;

        var requestPayload = new
        {
            chat,
            interactiveUpdates,
            newSession
        };

        var response = await _httpClient.PostAsJsonAsync("/api/llm/send", requestPayload);

        if (!response.IsSuccessStatusCode)
        {
            // Handle error
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to send chat: {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<ChatResult>();

        if (interactiveUpdates && result?.Message != null)
        {
            await _notificationService.DispatchNotification(
                NotificationMessageBuilder.CreateChatCompletion(
                    chat.Id,
                    result.Message.Content,
                    true),
                "ReceiveMessageUpdate");
        }

        return result;
    }

    public async Task<ChatResult?> AskMemory(Chat? chat, 
        Dictionary<string, string>? textData = null,
        Dictionary<string, string>? fileData = null,
        List<string>? memory = null)
    {
        if (chat == null || chat.Messages == null || !chat.Messages.Any())
            return null;

        var requestPayload = new
        {
            chat,
            textData,
            fileData,
            memory
        };

        var response = await _httpClient.PostAsJsonAsync("/api/llm/askmemory", requestPayload);

        if (!response.IsSuccessStatusCode)
        {
            // Handle error
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to ask memory: {error}");
        }

        return await response.Content.ReadFromJsonAsync<ChatResult>();
    }

    public async Task<List<string>> GetCurrentModels()
    {
        var response = await _httpClient.GetAsync("/api/llm/models");

        if (!response.IsSuccessStatusCode)
        {
            // Handle error
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to get models: {error}");
        }

        return await response.Content.ReadFromJsonAsync<List<string>>() ?? new List<string>();
    }

    public Task CleanSessionCache(string id)
    {
        // Notify the external AI server to clear the session cache.
        return _httpClient.DeleteAsync($"/api/llm/session/{id}");
    }
}