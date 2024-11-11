using System.Text.Json;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Services.Configuration;
using MaIN.Services.Models;
using MaIN.Services.Models.Ollama;
using MaIN.Services.Services.Abstract;
using Microsoft.Extensions.Options;

namespace MaIN.Services.Services;

public class OllamaService(
    IHttpClientFactory httpClientFactory,
    IOptions<MaINSettings> options) : IOllamaService
{
    public async Task<ChatResult?> Send(Chat? chat)
    {
        using var client = httpClientFactory.CreateClient();
        var response = await client.PostAsync($"{options.Value.OllamaUrl}/api/chat",
            new StringContent(JsonSerializer.Serialize(new ChatRequest()
            {
                Messages = chat.Messages.Select(x => new MessageDto()
                {
                    Content = x.Content,
                    Role = x.Role,
                    Images = x.Images
                }).ToList(),
                Model = chat.Model,
                Stream = chat.Stream,
            }), System.Text.Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to create completion for chat {chat.Id} with message " +
                                $"{chat.Messages.Last().Content}, status code {response.StatusCode}");
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ChatResult>(responseBody);
        return result;
    }
    
    public async Task<List<string>> GetCurrentModels()
    {
        using var client = httpClientFactory.CreateClient();
        var response = await client.GetAsync($"{options.Value.OllamaUrl}/api/tags");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to fetch models from Ollama, status code {response.StatusCode}");
        }
        
        var stringResponse = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ModelsOllamaResponse>(stringResponse);

        return result!.Models.Select(x => x.Name).ToList();
    }
}