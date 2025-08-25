using MaIN.Services.Utils;
using Microsoft.KernelMemory;
using System.Text.Json;

namespace MaIN.Services.Services.LLMService.Memory;

public class MemoryService : IMemoryService
{
    public async Task ImportDataToMemory(
        IKernelMemory memory,
        ChatMemoryOptions options,
        CancellationToken cancellationToken)
    {
        if (options.PreProcess)
        {
            await PreprocessAvailableDocuments(options, cancellationToken);
        }
        await ImportTextData(memory, options.TextData, cancellationToken);
        await ImportFilesData(memory, options.FilesData, cancellationToken);
        await ImportStreamData(memory, options.StreamData, cancellationToken);
        await ImportWebUrls(memory, options.WebUrls, cancellationToken);
        await ImportMemoryItems(memory, options.Memory, cancellationToken);
    }
    
    public string CleanResponseText(string text)
    {
        return text
            .Replace("Question:", string.Empty)
            .Replace("Assistant:", string.Empty);
    }

    private async Task ImportTextData(IKernelMemory memory, Dictionary<string, string>? textData,
        CancellationToken cancellationToken)
    {
        if (textData is null || textData.Count == 0)
            return;

        foreach (var item in textData)
        {
            var cleanedValue = JsonCleaner.CleanAndUnescape(item.Value);
            await memory.ImportTextAsync(cleanedValue!, item.Key, cancellationToken: cancellationToken);
        }
    }

    private async Task ImportFilesData(IKernelMemory memory, Dictionary<string, string>? fileData,
        CancellationToken cancellationToken)
    {
        if (fileData?.Any() != true)
            return;

        foreach (var item in fileData)
        {
            await memory.ImportDocumentAsync(item.Value, item.Key, cancellationToken: cancellationToken);
        }
    }

    private async Task ImportStreamData(IKernelMemory memory, Dictionary<string, FileStream>? streamData,
        CancellationToken cancellationToken)
    {
        if (streamData?.Any() != true)
            return;

        foreach (var item in streamData)
        {
            await memory.ImportDocumentAsync(item.Value, item.Key, cancellationToken: cancellationToken);
        }
    }

    private async Task ImportWebUrls(IKernelMemory memory, List<string>? webUrls, CancellationToken cancellationToken)
    {
        if (webUrls is null || webUrls.Count == 0)
            return;

        foreach (var item in webUrls)
        {
            await memory.ImportWebPageAsync(item, cancellationToken: cancellationToken);
        }
    }

    private async Task ImportMemoryItems(IKernelMemory memory,
        List<string>? memoryItems,
        CancellationToken cancellationToken)
    {
        if (memoryItems is null || memoryItems.Count == 0)
            return;

        foreach (var item in memoryItems.Select((value, i) => (value, i)))
        {
            await memory.ImportTextAsync(
                item.value,
                $"ANSWER_MEMORY_{item.i + 1}-{memoryItems.Count}",
                cancellationToken: cancellationToken);
        }
    }
    
    private static async Task PreprocessAvailableDocuments(ChatMemoryOptions options, CancellationToken cancellationToken)
    {
        foreach (var file in options.FilesData!)
        {
            options.TextData!.Add(file.Key ,DocumentProcessor.ProcessDocument(file.Value));
            options.FilesData = [];
        }

        foreach (var stream in options.StreamData!)
        {
            var fileStream = new FileStream(Path.GetTempPath()+$".{stream.Key}", FileMode.Create, FileAccess.Write);
            await stream.Value.CopyToAsync(fileStream, cancellationToken);
            await fileStream.DisposeAsync();
            options.TextData!.Add(stream.Key, DocumentProcessor.ProcessDocument(Path.GetTempPath()+$".{stream.Key}"));
            options.StreamData = [];
        }
    }

}