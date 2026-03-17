using LLama;
using LLama.Sampling;
using MaIN.Domain.Entities;
using MaIN.Domain.Models.Abstract;
using MaIN.Services.Constants;
using InferenceParams = LLama.Common.InferenceParams;

namespace MaIN.Services.Services.LLMService.Utils;

/// <summary>
/// Helper class for chat-related operations
/// </summary>
public static class ChatHelper
{
    private static readonly HashSet<string> ImageExtensions =
    [
        ".jpg", ".jpeg", ".png", ".gif", ".webp",
        ".bmp", ".tiff", ".tif", ".heic", ".heif", ".avif"
    ];

    /// <summary>
    /// Extracts image files from message.Files into message.Images and removes them from Files.
    /// This must be called before HasFiles() so images are not mistakenly routed to the RAG/memory path.
    /// </summary>
    public static async Task ExtractImageFromFiles(Message message)
    {
        if (message.Files == null || message.Files.Count == 0)
            return;

        var imageFiles = message.Files
            .Where(f => ImageExtensions.Contains(f.Extension.ToLowerInvariant()))
            .ToList();

        if (imageFiles.Count == 0)
            return;

        var imageBytesList = new List<byte[]>();
        foreach (var imageFile in imageFiles)
        {
            if (imageFile.StreamContent != null)
            {
                using var ms = new MemoryStream();
                imageFile.StreamContent.Position = 0;
                await imageFile.StreamContent.CopyToAsync(ms);
                imageBytesList.Add(ms.ToArray());
            }
            else if (imageFile.Path != null)
            {
                imageBytesList.Add(await File.ReadAllBytesAsync(imageFile.Path));
            }

            message.Files.Remove(imageFile);
        }

        message.Images = imageBytesList;

        if (message.Files.Count == 0)
            message.Files = null;
    }


    /// <summary>
    /// Generates final prompt including additional prompt if needed
    /// </summary>
    public static string GetFinalPrompt(Message message, AIModel model, bool startSession)
    {
        var additionalPrompt = (model as IReasoningModel)?.AdditionalPrompt;
        return startSession && additionalPrompt != null 
            ? $"{message.Content}{additionalPrompt}" 
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
                Temperature = chat.LocalParams!.Temperature,
                TopK = chat.LocalParams!.TopK,
                TopP = chat.LocalParams!.TopP
            },
            AntiPrompts = [model.Vocab.EOT?.ToString() ?? "User:"],
            TokensKeep = chat.LocalParams!.TokensKeep,
            MaxTokens = chat.LocalParams!.MaxTokens
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

        var preProcess = message.Properties.CheckProperty(ServiceConstants.Properties.PreProcessProperty);
        
        return new ChatMemoryOptions
        {
            TextData = textData,
            FilesData = fileData,
            StreamData = streamData,
            PreProcess = preProcess
        };
    }
}