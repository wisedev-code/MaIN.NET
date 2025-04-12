using Microsoft.KernelMemory;

namespace MaIN.Services.Services.LLMService.Memory;

public class MemoryService(IMemoryFactory memoryFactory) : IMemoryService
{
    public async Task<string> ProcessMemoryRequest(
        string modelName,
        string query,
        ChatMemoryOptions options,
        CancellationToken cancellationToken)
    {
        var memory = memoryFactory.CreateMemory(null, modelName);

        await ImportDataToMemory(memory, options, cancellationToken);

        var result = await memory.AskAsync(query, cancellationToken: cancellationToken);
        await memory.DeleteIndexAsync(cancellationToken: cancellationToken);

        return CleanResponseText(result.Result);
    }

    public async Task ImportDataToMemory(
        IKernelMemory memory,
        ChatMemoryOptions options,
        CancellationToken cancellationToken)
    {
        await ImportTextData(memory, options.TextData, cancellationToken);
        await ImportFileData(memory, options.FileData, cancellationToken);
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

    private Task ImportTextData(IKernelMemory memory, Dictionary<string, string>? textData,
        CancellationToken cancellationToken)
    {
        if (textData?.Any() != true)
            return Task.CompletedTask;

        return Task.WhenAll(
            textData.Select(item =>
                memory.ImportTextAsync(item.Value, item.Key, cancellationToken: cancellationToken))
        );
    }

    private async Task ImportFileData(IKernelMemory memory, Dictionary<string, string>? fileData,
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
}