using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using MaIN.Services.Dtos;
using MaIN.Services.Services.ImageGenServices;
using FileInfo = MaIN.Domain.Entities.FileInfo;

namespace MaIN.Services.Mappers;

public static class ChatMapper
{
    public static ChatDto ToDto(this Chat chat)
        => new()
        {
            Id = chat.Id,
            Name = chat.Name,
            Model = chat.ModelId,
            Messages = [.. chat.Messages.Select(m => m.ToDto())],
            ImageGen = chat.ImageGen,
            Type = Enum.Parse<ChatTypeDto>(chat.Type.ToString()),
            Properties = chat.Properties
        };

    private static MessageDto ToDto(this Message message)
        => new()
        {
            Content = message.Content,
            Role = message.Tool ? "System" : message.Role,
            Images = message.Image,
            Time = message.Time,
            Properties = message.Properties,
            Files = message.Files?.Select(x => new FileInfoDto()
            {
                Content = x.Content ?? string.Empty,
                StreamContent = x.StreamContent,
                Name = x.Name,
                Extension = x.Extension
            }) as FileInfoDto[]
        };

    public static Chat ToDomain(this ChatDto chat)
        => new()
        {
            Id = chat.Id!,
            Name = chat.Name!,
            ModelId = chat.Model!,
            Messages = chat.Messages?.Select(m => m.ToDomain()).ToList()!,
            ImageGen = chat.Model == ImageGenService.LocalImageModels.FLUX,
            Type = Enum.Parse<ChatType>(chat.Type.ToString()),
            Properties = chat.Properties
        };

    private static Message ToDomain(this MessageDto message)
        => new()
        {
            Content = message.Content,
            Role = message.Role,
            Image = message.Images,
            Time = message.Time,
            Type = Enum.Parse<MessageType>(message.Type),
            Properties = message.Properties,
            Files = message.Files?.Select(x => new FileInfo()
            {
                Content = x.Content,
                Name = x.Name,
                Extension = x.Extension
            }).ToList()
        };
}
