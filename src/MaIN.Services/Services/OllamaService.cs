using System.Text.Json;
using MaIN.Domain.Entities;
using MaIN.Models;
using MaIN.Services.Models;
using MaIN.Services.Models.Ollama;
using MaIN.Services.Services.Abstract;

namespace MaIN.Services.Services;

public class OllamaService(IHttpClientFactory httpClientFactory) : IOllamaService
{
    public async Task<ChatOllamaResult?> Send(Chat? chat)
    {
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

        var responseBody = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ChatOllamaResult>(responseBody);
        return result;
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
    
    private static string GetLocalhost() =>
        Environment.GetEnvironmentVariable("LocalHost") ?? "http://localhost";
}