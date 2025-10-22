using System.Text.Json;
using LLama.Batched;
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

    private static MessageDocument ToDocument(this Message message)
        => new MessageDocument()
        {
            Content = message.Content,
            Role = message.Role,
            Time = message.Time,
            MessageType = message.Type.ToString(),
            Images = message.Image,
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
            Backend = chat.Backend,
            ToolsConfiguration = chat.ToolsConfiguration,  
            MemoryParams = chat.MemoryParams.ToDocument(),
            InferenceParams = chat.InterferenceParams.ToDocument(),
            ConvState = chat.ConversationState,
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
            Backend = chat.Backend,
            Properties = chat.Properties,
            ToolsConfiguration = chat.ToolsConfiguration,
            ConversationState = chat.ConvState as Conversation.State,
            MemoryParams = chat.MemoryParams!.ToDomain(),
            InterferenceParams = chat.InferenceParams!.ToDomain(),
            Interactive = chat.Interactive,
            Translate = chat.Translate,
            Type = Enum.Parse<ChatType>(chat.Type.ToString())
        };

    private static Message ToDomain(this MessageDocument message)
        => new()
        {
            Content = message.Content,
            Tool = message.Tool,
            Time = message.Time,
            Type = Enum.Parse<MessageType>(message.MessageType),
            Tokens = message.Tokens.Select(x => x.ToDomain()).ToList(),
            Role = message.Role,
            Image = message.Images,
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
        => new InferenceParams
        {
            Temperature = inferenceParams.Temperature,
            ContextSize = inferenceParams.ContextSize,
            GpuLayerCount = inferenceParams.GpuLayerCount,
            SeqMax = inferenceParams.SeqMax,
            BatchSize = inferenceParams.BatchSize,
            UBatchSize = inferenceParams.UBatchSize,
            Embeddings = inferenceParams.Embeddings,
            TypeK = inferenceParams.TypeK,
            TypeV = inferenceParams.TypeV,
            TokensKeep = inferenceParams.TokensKeep,
            MaxTokens = inferenceParams.MaxTokens,
            TopK = inferenceParams.TopK,
            TopP = inferenceParams.TopP,
            Grammar = inferenceParams.Grammar
        };
    
    private static MemoryParams ToDomain(this MemoryParamsDocument memoryParams)
        => new MemoryParams
        {
            Temperature = memoryParams.Temperature,
            AnswerTokens = memoryParams.AnswerTokens,
            MultiModalMode = memoryParams.MultiModalMode,
            ContextSize = memoryParams.ContextSize,
            GpuLayerCount = memoryParams.GpuLayerCount,
            MaxMatchesCount = memoryParams.MaxMatchesCount,
            FrequencyPenalty = memoryParams.FrequencyPenalty,
            Grammar = memoryParams.Grammar
        };

    private static InferenceParamsDocument ToDocument(this InferenceParams inferenceParams)
        => new InferenceParamsDocument
        {
            Temperature = inferenceParams.Temperature,
            ContextSize = inferenceParams.ContextSize,
            GpuLayerCount = inferenceParams.GpuLayerCount,
            SeqMax = inferenceParams.SeqMax,
            BatchSize = inferenceParams.BatchSize,
            UBatchSize = inferenceParams.UBatchSize,
            Embeddings = inferenceParams.Embeddings,
            TypeK = inferenceParams.TypeK,
            TypeV = inferenceParams.TypeV,
            TokensKeep = inferenceParams.TokensKeep,
            MaxTokens = inferenceParams.MaxTokens,
            TopK = inferenceParams.TopK,
            TopP = inferenceParams.TopP,
            Grammar = inferenceParams.Grammar
        };
    
    private static MemoryParamsDocument ToDocument(this MemoryParams memoryParams)
        => new MemoryParamsDocument
        {
            Temperature = memoryParams.Temperature,
            AnswerTokens = memoryParams.AnswerTokens,
            MultiModalMode = memoryParams.MultiModalMode,
            ContextSize = memoryParams.ContextSize,
            GpuLayerCount = memoryParams.GpuLayerCount,
            MaxMatchesCount = memoryParams.MaxMatchesCount,
            FrequencyPenalty = memoryParams.FrequencyPenalty,
            Grammar = memoryParams.Grammar
        };
}