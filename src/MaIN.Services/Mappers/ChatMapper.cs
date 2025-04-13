using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using MaIN.Infrastructure.Models;
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
            Model = chat.Model,
            Messages = chat.Messages.Select(m => m.ToDto()).ToList(),
            Visual = chat.Visual,
            Type = Enum.Parse<ChatTypeDto>(chat.Type.ToString()),
            Properties = chat.Properties
        };

    private static MessageDto ToDto(this Message message)
        => new MessageDto()
        {
            Content = message.Content,
            Role = message.Tool ? "System" : message.Role,
            Images = message.Images,
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
            Model = chat.Model!,
            Messages = chat.Messages?.Select(m => m.ToDomain()).ToList()!,
            Visual = chat.Model == ImageGenService.LocalImageModels.FLUX,
            Type = Enum.Parse<ChatType>(chat.Type.ToString()),
            Properties = chat.Properties
        };

    private static Message ToDomain(this MessageDto message)
        => new()
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

    private static MessageDocument ToDocument(this Message message)
        => new MessageDocument()
        {
            Content = message.Content,
            Role = message.Role,
            Time = message.Time,
            Images = message.Images,
            Tokens = message.Tokens.Select(x => x.ToDocument()).ToList(),
            Properties = message.Properties,
            Tool = message.Tool,
            Files = (message.Files?.Select(x => x.Content).ToArray() ?? [])!
        };

    public static ChatDocument ToDocument(this Chat chat)
        => new ChatDocument()
        {
            Id = chat.Id,
            Name = chat.Name,
            Model = chat.Model,
            Messages = chat.Messages.Select(m => m.ToDocument()).ToList(),
            Visual = chat.Visual,
            InferenceParams = chat.InterferenceParams.ToDocument(),
            Properties = chat.Properties,
            Interactive = chat.Interactive,
            Translate = chat.Translate,
            Type = Enum.Parse<ChatTypeDocument>(chat.Type.ToString())
        };

    public static Chat ToDomain(this ChatDocument chat)
        => new()
        {
            Id = chat.Id,
            Name = chat.Name,
            Model = chat.Model,
            Messages = chat.Messages.Select(m => m.ToDomain()).ToList(),
            Visual = chat.Visual,
            Properties = chat.Properties,
            InterferenceParams = chat.InferenceParams!.ToDomain(),
            Interactive = chat.Interactive,
            Translate = chat.Translate,
            Type = Enum.Parse<ChatType>(chat.Type.ToString())
        };

    private static Message ToDomain(this MessageDocument message)
        => new Message()
        {
            Content = message.Content,
            Tool = message.Tool,
            Time = message.Time,
            Tokens = message.Tokens.Select(x => x.ToDomain()).ToList(),
            Role = message.Role,
            Images = message.Images,
            Properties = message.Properties,
        };

    private static LLMTokenValueDocument ToDocument(this LLMTokenValue llmTokenValue)
        => new()
        {
            Text = llmTokenValue.Text,
            Type = llmTokenValue.Type
        };

    private static LLMTokenValue ToDomain(this LLMTokenValueDocument llmTokenValue)
        => new LLMTokenValue()
        {
            Text = llmTokenValue.Text,
            Type = llmTokenValue.Type
        };

    private static InferenceParams ToDomain(this InferenceParamsDocument inferenceParams)
        => new InferenceParams()
        {
            Temperature = inferenceParams.Temperature,
            ContextSize = inferenceParams.ContextSize
        };

    private static InferenceParamsDocument ToDocument(this InferenceParams inferenceParams)
        => new InferenceParamsDocument()
        {
            Temperature = inferenceParams.Temperature,
            ContextSize = inferenceParams.ContextSize
        };
}