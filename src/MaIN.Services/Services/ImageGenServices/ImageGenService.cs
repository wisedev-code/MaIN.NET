using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;

namespace MaIN.Services.Services.ImageGenServices;

public class ImageGenService(
    IHttpClientFactory httpClientFactory,
    MaINSettings settings)
    : IImageGenService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly MaINSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    public async Task<ChatResult?> Send(Chat chat)
    {
        var client = _httpClientFactory.CreateClient(ServiceConstants.HttpClients.ImageGenClient);
        client.Timeout = TimeSpan.FromMinutes(ServiceConstants.Defaults.HttpImageModelTimeoutInMinutes);
        
        string constructedMessage = BuildMessageContent(chat.Messages);
        string requestUrl = $"{_settings.ImageGenUrl}/generate/{constructedMessage}";
        
        using var response = await client.PostAsync(requestUrl, null);
        response.EnsureSuccessStatusCode(); 
        
        byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
        return CreateChatResult(imageBytes);
    }
    
    private static string BuildMessageContent(ICollection<Message> messages)
    {
        return messages
            .Select((msg, index) => index == 0 ? msg.Content : $"&& {msg.Content}")
            .Aggregate((current, next) => $"{current} {next}");
    }

    private static ChatResult CreateChatResult(byte[] imageBytes)
    {
        return new ChatResult
        {
            Done = true,
            Message = new Message
            {
                Content = ServiceConstants.Messages.GeneratedImageContent,
                Role = ServiceConstants.Roles.Assistant,
                Image = imageBytes,
                Type = MessageType.Image
            },
            Model = LocalImageModels.FLUX,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    internal struct LocalImageModels
    {
        public const string FLUX = "FLUX.1_Shnell";
    }
}