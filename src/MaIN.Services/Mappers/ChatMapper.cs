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
            Stream = chat.Stream
        };

    public static MessageDto ToDto(this Message message)
        => new MessageDto()
        {
            Content = message.Content,
            Role = message.Role
        };

    public static Chat ToDomain(this ChatDto chat)
        => new Chat()
        {
            Id = chat.Id,
            Name = chat.Name,
            Model = chat.Model,
            Messages = chat.Messages?.Select(m => m.ToDomain()).ToList(),
            Stream = chat.Stream
        };

    public static Message ToDomain(this MessageDto message)
        => new Message()
        {
            Content = message.Content,
            Role = message.Role
        };

    public static MessageDocument ToDocument(this Message message)
        => new MessageDocument()
        {
            Content = message.Content,
            Role = message.Role
        };

    public static ChatDocument ToDocument(this Chat chat)
        => new ChatDocument()
        {
            Id = chat.Id,
            Name = chat.Name,
            Model = chat.Model,
            Messages = chat.Messages.Select(m => m.ToDocument()).ToList(),
            Stream = chat.Stream
        };

    public static Chat ToDomain(this ChatDocument chat)
        => new Chat()
        {
            Id = chat.Id,
            Name = chat.Name,
            Model = chat.Model,
            Messages = chat.Messages.Select(m => m.ToDomain()).ToList(),
            Stream = chat.Stream
        };

    public static Message ToDomain(this MessageDocument message)
        => new Message()
        {
            Content = message.Content,
            Role = message.Role
        };
}