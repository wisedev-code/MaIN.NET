using LLama;
using LLama.Sampling;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using InferenceParams = LLama.Common.InferenceParams;

namespace MaIN.Services.Services.LLMService.Utils;

/// <summary>
/// Helper class for chat-related operations
/// </summary>
public static class ChatHelper
{
    /// <summary>
    /// Generates final prompt including additional prompt if needed
    /// </summary>
    public static string GetFinalPrompt(Message message, Model model, bool startSession)
    {
        return startSession && model.AdditionalPrompt != null 
            ? $"{message.Content}{model.AdditionalPrompt}" 
            : message.Content;
    }

    /// <summary>
    /// Creates inference parameters for a chat
    /// </summary>
    public static InferenceParams CreateInferenceParams(Chat chat, LLamaWeights model)
    {
        return new InferenceParams
        {
            SamplingPipeline = new DefaultSamplingPipeline
            {
                Temperature = chat.InterferenceParams.Temperature,
                TopK = chat.InterferenceParams.TopK,
                TopP = chat.InterferenceParams.TopP
            },
            AntiPrompts = [model.Vocab.EOT?.ToString() ?? "User:"],
            TokensKeep = chat.InterferenceParams.TokensKeep,
            MaxTokens = chat.InterferenceParams.MaxTokens
        };
    }

    /// <summary>
    /// Checks if a message contains files
    /// </summary>
    public static bool HasFiles(Message message)
    {
        return message.Files?.Any() ?? false;
    }

    /// <summary>
    /// Extracts memory options from a message with files
    /// </summary>
    public static ChatMemoryOptions ExtractMemoryOptions(Message message)
    {
        if (message.Files == null)
            return new ChatMemoryOptions();
            
        var textData = message.Files
            .Where(x => x.Content != null)
            .ToDictionary(x => x.Name, x => x.Content!);
        
        var fileData = message.Files
            .Where(x => x.Path != null)
            .ToDictionary(x => x.Name, x => x.Path!);
        
        var streamData = message.Files
            .Where(x => x.StreamContent != null)
            .ToDictionary(x => x.Name, x => x.StreamContent!);
        
        return new ChatMemoryOptions
        {
            TextData = textData,
            FileData = fileData,
            StreamData = streamData
        };
    }
}