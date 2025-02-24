using MaIN.Domain.Entities;
using MaIN.Infrastructure.Models;
using MaIN.Models;
using MaIN.Services.Models;
using MaIN.Services.Services;
using FileInfo = MaIN.Domain.Entities.FileInfo;

namespace MaIN.Services.Mappers;

public static class ChatMapper
{
    public static ChatDto ToDto(this Chat chat)
        => new ChatDto()
        {
            Id = chat.Id,
            Name = chat.Name,
            Model = chat.Model,
            Messages = chat.Messages.Select(m => m.ToDto()).ToList(),
            Visual = chat.Visual,
            Stream = chat.Stream,
            Type = Enum.Parse<ChatTypeDto>(chat.Type.ToString()),
            Properties = chat.Properties
        };

    public static MessageDto ToDto(this Message message)
        => new MessageDto()
        {
            Content = message.Content,
            Role = message.Tool ? "System" : message.Role,
            Images = message.Images,
            Time = message.Time,
            Properties = message.Properties,
            Files = message.Files?.Select(x => new FileInfoDto()
            {
                Content = x.Content,
                Name = x.Name,
                Extension = x.Extension
            }) as FileInfoDto[]
        };

    public static Chat ToDomain(this ChatDto chat)
        => new Chat()
        {
            Id = chat.Id,
            Name = chat.Name,
            Model = chat.Model,
            Messages = chat.Messages?.Select(m => m.ToDomain()).ToList(),
            Visual = chat.Model == ImageGenService.Models.FLUX,
            Stream = chat.Stream,
            Type = Enum.Parse<ChatType>(chat.Type.ToString()),
            Properties = chat.Properties
        };

    public static Message ToDomain(this MessageDto message)
        => new Message()
        {
            Content = message.Content,
            Role = message.Role,
            Images = message.Images,
            Time = message.Time,
            Properties = message.Properties,
            Files = message.Files?.Select(x => new FileInfo()
            {
                Content = x.Content,
                Name = x.Name,
                Extension = x.Extension
            }).ToList()
        };

    public static MessageDocument ToDocument(this Message message)
        => new MessageDocument()
        {
            Content = message.Content,
            Role = message.Role,
            Time = message.Time,
            Images = message.Images,
            Properties = message.Properties,
            Tool = message.Tool,
            Files = message.Files?.Select(x => x.Content).ToArray() ?? []
        };

    public static ChatDocument ToDocument(this Chat chat)
        => new ChatDocument()
        {
            Id = chat.Id,
            Name = chat.Name,
            Model = chat.Model,
            Messages = chat.Messages.Select(m => m.ToDocument()).ToList(),
            Visual = chat.Visual,
            Properties = chat.Properties,
            Stream = chat.Stream,
            Interactive = chat.Interactive,
            Translate = chat.Translate,
            Type = Enum.Parse<ChatTypeDocument>(chat.Type.ToString())
        };

    public static Chat ToDomain(this ChatDocument chat)
        => new Chat()
        {
            Id = chat.Id,
            Name = chat.Name,
            Model = chat.Model,
            Messages = chat.Messages.Select(m => m.ToDomain()).ToList(),
            Visual = chat.Visual,
            Stream = chat.Stream,
            Properties = chat.Properties,
            Interactive = chat.Interactive,
            Translate = chat.Translate,
            Type = Enum.Parse<ChatType>(chat.Type.ToString())
        };

    public static Message ToDomain(this MessageDocument message)
        => new Message()
        {
            Content = message.Content,
            Tool = message.Tool,
            Time = message.Time,
            Role = message.Role,
            Images = message.Images,
            Properties = message.Properties,
        };
}