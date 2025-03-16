using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Services.Models;
using MaIN.Services.Services.Abstract;

namespace MaIN.Services.Services.ImageGenServices;

public class ImageGenService(
    IHttpClientFactory httpClientFactory,
    MaINSettings options) : IImageGenService
{
    public async Task<ChatResult?> Send(Chat chat)
    {
        using var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(5);
        var constructedMessage = (chat.Messages
            .Select((msg, index) => index == 0 ? msg.Content
                : $"&& {msg.Content}")
            .Aggregate((current, next) => $"{current} {next}"));
        var response = await client.PostAsync($"{options.ImageGenUrl}/generate/{constructedMessage}", null);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to create completion for chat {chat.Id} with message " +
                                $"{chat.Messages.Last().Content}, status code {response.StatusCode}");
        }

        byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
        var result = new ChatResult()
        {
            Done = true,
            Message = new MessageDto()
            {
                Content = "Generated Image:",
                Role = "Assistant",
                Images = imageBytes
            },
            Model = Models.FLUX,
            CreatedAt = DateTime.Now
        };
        
        return result;
    }

    public struct Models
    {
        public const string FLUX = "FLUX.1_Shnell";
    }
}