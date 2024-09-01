using MaIN.Domain.Entities;
using MaIN.Infrastructure.Models;
using MaIN.Models;
using MaIN.Services.Models;

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
            Stream = chat.Stream,
            Type = Enum.Parse<ChatTypeDto>(chat.Type.ToString()),
            Properties = chat.Properties
        };

    public static MessageDto ToDto(this Message message)
        => new MessageDto()
        {
            Content = message.Content,
            Role = message.Role,
            Images = message.Images
        };

    public static Chat? ToDomain(this ChatDto chat)
        => new Chat()
        {
            Id = chat.Id,
            Name = chat.Name,
            Model = chat.Model,
            Messages = chat.Messages?.Select(m => m.ToDomain()).ToList(),
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
        };

    public static MessageDocument ToDocument(this Message message)
        => new MessageDocument()
        {
            Content = message.Content,
            Role = message.Role
        };

    public static ChatDocument ToDocument(this Chat? chat)
        => new ChatDocument()
        {
            Id = chat.Id,
            Name = chat.Name,
            Model = chat.Model,
            Messages = chat.Messages.Select(m => m.ToDocument()).ToList(),
            Properties = chat.Properties,
            Stream = chat.Stream,
            Type = Enum.Parse<ChatTypeDocument>(chat.Type.ToString())
        };

    public static Chat ToDomain(this ChatDocument chat)
        => new Chat()
        {
            Id = chat.Id,
            Name = chat.Name,
            Model = chat.Model,
            Messages = chat.Messages.Select(m => m.ToDomain()).ToList(),
            Stream = chat.Stream,
            Properties = chat.Properties,
            Type = Enum.Parse<ChatType>(chat.Type.ToString())
        };

    public static Message ToDomain(this MessageDocument message)
        => new Message()
        {
            Content = message.Content,
            Role = message.Role,
            Images = message.Images
        };
}