using LLama.Native;
using LLamaSharp.KernelMemory;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;

namespace MaIN.Services.Services.LLMService.Memory;

public class MemoryService : IMemoryService
{
    public async Task ImportDataToMemory((IKernelMemory km, ITextEmbeddingGenerator? generator) memory,
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

    private async Task ImportTextData((IKernelMemory km, ITextEmbeddingGenerator? generator) memory, Dictionary<string, string>? textData,
        CancellationToken cancellationToken)
    {
        if (textData is null || textData.Count == 0)
            return;

        foreach (var item in textData)
        {
            PreImport(memory.generator);
            var cleanedValue = JsonCleaner.CleanAndUnescape(item.Value);
            await memory.km.ImportTextAsync(cleanedValue!, item.Key, cancellationToken: cancellationToken);
            PostImport(memory.generator);
        }
    }

    private async Task ImportFilesData((IKernelMemory km, ITextEmbeddingGenerator? generator) memory, Dictionary<string, string>? fileData,
        CancellationToken cancellationToken)
    {
        if (fileData?.Any() != true)
            return;

        
        foreach (var item in fileData)
        {
            PreImport(memory.generator);
            await memory.km.ImportDocumentAsync(item.Value, item.Key, cancellationToken: cancellationToken);
            PostImport(memory.generator);
        }
    }
    

    private async Task ImportStreamData((IKernelMemory km, ITextEmbeddingGenerator? generator) memory, Dictionary<string, FileStream>? streamData,
        CancellationToken cancellationToken)
    {
        if (streamData?.Any() != true)
            return;

        foreach (var item in streamData)
        {
            PreImport(memory.generator);
            await memory.km.ImportDocumentAsync(item.Value, item.Key, cancellationToken: cancellationToken);
            PostImport(memory.generator);
        }
    }

    private async Task ImportWebUrls((IKernelMemory km, ITextEmbeddingGenerator? generator) memory, List<string>? webUrls, CancellationToken cancellationToken)
    {
        if (webUrls is null || webUrls.Count == 0)
            return;

        foreach (var item in webUrls)
        {
            PreImport(memory.generator);
            await memory.km.ImportWebPageAsync(item, cancellationToken: cancellationToken);
            PostImport(memory.generator);
        }
    }

    private async Task ImportMemoryItems((IKernelMemory km, ITextEmbeddingGenerator? generator) memory,
        List<string>? memoryItems,
        CancellationToken cancellationToken)
    {
        if (memoryItems is null || memoryItems.Count == 0)
            return;

        foreach (var item in memoryItems.Select((value, i) => (value, i)))
        {
            PreImport(memory.generator);
            await memory.km.ImportTextAsync(
                item.value,
                $"ANSWER_MEMORY_{item.i + 1}-{memoryItems.Count}",
                cancellationToken: cancellationToken);
            PostImport(memory.generator);
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
    
    private void PostImport(ITextEmbeddingGenerator? memoryGenerator)
    {
        if (memoryGenerator is LLamaSharpTextEmbeddingOwn llamaGenerator)
        {
            llamaGenerator._embedder.Context.Dispose();
            llamaGenerator._embedder.isContextDisposed = true;
        }
    }

    private void PreImport(ITextEmbeddingGenerator? memoryGenerator)
    {
        if (memoryGenerator is LLamaSharpTextEmbeddingOwn { _embedder.isContextDisposed: true } llamaGenerator)
        {
            llamaGenerator._embedder.Context = llamaGenerator
                ._embedder
                ._weights
                .CreateContext(llamaGenerator.@params!);
            llamaGenerator._embedder.isContextDisposed = false;
            NativeApi.llama_set_embeddings(llamaGenerator._embedder.Context.NativeHandle, true);

        }
    }

}