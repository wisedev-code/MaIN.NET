using LLama.Batched;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using MaIN.Infrastructure.Models;

namespace MaIN.Infrastructure.Mappers;

internal static class ChatDocumentMapper
{
    internal static ChatDocument ToDocument(this Chat chat) => new()
    {
        Id = chat.Id,
        Name = chat.Name,
        Model = chat.ModelId,
        Messages = [.. chat.Messages.Select(m => m.ToDocument())],
        ImageGen = chat.ImageGen,
        ToolsConfiguration = chat.ToolsConfiguration,
        MemoryParams = chat.MemoryParams.ToDocument(),
        InferenceParams = chat.InterferenceParams.ToDocument(),
        ConvState = chat.ConversationState,
        Properties = chat.Properties,
        Interactive = chat.Interactive,
        Translate = chat.Translate,
        Type = Enum.Parse<ChatTypeDocument>(chat.Type.ToString())
    };

    internal static Chat ToDomain(this ChatDocument chat) => new()
    {
        Id = chat.Id,
        Name = chat.Name,
        ModelId = chat.Model,
        Messages = [.. chat.Messages.Select(m => m.ToDomain())],
        ImageGen = chat.ImageGen,
        Properties = chat.Properties,
        ToolsConfiguration = chat.ToolsConfiguration,
        ConversationState = chat.ConvState as Conversation.State,
        MemoryParams = chat.MemoryParams!.ToDomain(),
        InterferenceParams = chat.InferenceParams!.ToDomain(),
        Interactive = chat.Interactive,
        Translate = chat.Translate,
        Type = Enum.Parse<ChatType>(chat.Type.ToString())
    };

    private static MessageDocument ToDocument(this Message message) => new()
    {
        Content = message.Content,
        Role = message.Role,
        Time = message.Time,
        MessageType = message.Type.ToString(),
        Images = message.Image,
        Tokens = [.. message.Tokens.Select(x => x.ToDocument())],
        Properties = message.Properties,
        Tool = message.Tool,
        Files = (message.Files?.Select(x => x.Content).ToArray() ?? [])!
    };

    private static Message ToDomain(this MessageDocument message) => new()
    {
        Content = message.Content,
        Tool = message.Tool,
        Time = message.Time,
        Type = Enum.Parse<MessageType>(message.MessageType),
        Tokens = [.. message.Tokens.Select(x => x.ToDomain())],
        Role = message.Role,
        Image = message.Images,
        Properties = message.Properties,
    };

    private static LLMTokenValueDocument ToDocument(this LLMTokenValue llmTokenValue) => new()
    {
        Text = llmTokenValue.Text,
        Type = llmTokenValue.Type
    };

    private static LLMTokenValue ToDomain(this LLMTokenValueDocument llmTokenValue) => new()
    {
        Text = llmTokenValue.Text,
        Type = llmTokenValue.Type
    };

    private static InferenceParamsDocument ToDocument(this InferenceParams inferenceParams) => new()
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

    private static InferenceParams ToDomain(this InferenceParamsDocument inferenceParams) => new()
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

    private static MemoryParamsDocument ToDocument(this MemoryParams memoryParams) => new()
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

    private static MemoryParams ToDomain(this MemoryParamsDocument memoryParams) => new()
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
