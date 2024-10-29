using System.Text.Json;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Services.Models;
using MaIN.Services.Models.Ollama;
using MaIN.Services.Services.Abstract;
using Microsoft.Extensions.Options;

namespace MaIN.Services.Services;

public class ImageGenService(
    IHttpClientFactory httpClientFactory,
    IOptions<MainSettings> options) : IImageGenService
{
    public async Task<ChatResult?> Send(Chat? chat)
    {
        using var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(5);
        var constructedMessage = (chat?.Messages != null
            ? chat.Messages
                .Select((msg, index) => index == 0 ? msg.Content.Replace("~$~AGENT_INTERNAL_MESSAGE~$~", string.Empty)
                    : $"&& {msg.Content.Replace("~$~AGENT_INTERNAL_MESSAGE~$~", string.Empty)}")
                .Aggregate((current, next) => $"{current} {next}")
            : string.Empty)!;
        var response = await client.PostAsync($"{options.Value.ImageGenUrl}/generate/{constructedMessage}", null);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to create completion for chat {chat?.Id} with message " +
                                $"{chat.Messages?.Last().Content}, status code {response.StatusCode}");
        }

        byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
        string base64String = Convert.ToBase64String(imageBytes);
        var result = new ChatResult()
        {
            Done = true,
            Message = new MessageDto()
            {
                Content = "Generated Image:",
                Role = "assistant",
                Images = [base64String]
            },
            Model = Models.FLUX,
            CreatedAt = DateTime.Now
        };
        
        return result;
    }

    public Task<List<string>> GetCurrentModels()
    {
        throw new NotImplementedException();
    }

    public struct Models
    {
        public const string FLUX = "FLUX.1_Shnell";
    }
}