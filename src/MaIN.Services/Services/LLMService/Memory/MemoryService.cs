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
        await Task.WhenAll(
            ImportTextData(memory, options.TextData, cancellationToken),
            ImportFileData(memory, options.FileData, cancellationToken),
            ImportStreamData(memory, options.StreamData, cancellationToken),
            ImportWebUrls(memory, options.WebUrls, cancellationToken),
            ImportMemoryItems(memory, options.Memory, cancellationToken)
        );
    }

    public string CleanResponseText(string text)
    {
        return text
            .Replace("Question:", string.Empty)
            .Replace("Assistant:", string.Empty);
    }
    
    private Task ImportTextData(IKernelMemory memory, Dictionary<string, string>? textData, CancellationToken cancellationToken)
    {
        if (textData?.Any() != true)
            return Task.CompletedTask;
            
        return Task.WhenAll(
            textData.Select(item => 
                memory.ImportTextAsync(item.Value, item.Key, cancellationToken: cancellationToken))
        );
    }
    
    private Task ImportFileData(IKernelMemory memory, Dictionary<string, string>? fileData, CancellationToken cancellationToken)
    {
        if (fileData?.Any() != true)
            return Task.CompletedTask;
            
        return Task.WhenAll(
            fileData.Select(item => 
                memory.ImportDocumentAsync(item.Value, item.Key, cancellationToken: cancellationToken))
        );
    }
    
    private Task ImportStreamData(IKernelMemory memory, Dictionary<string, FileStream>? streamData, CancellationToken cancellationToken)
    {
        if (streamData?.Any() != true)
            return Task.CompletedTask;
            
        return Task.WhenAll(
            streamData.Select(item => 
                memory.ImportDocumentAsync(item.Value, item.Key, cancellationToken: cancellationToken))
        );
    }
    
    private Task ImportWebUrls(IKernelMemory memory, List<string>? webUrls, CancellationToken cancellationToken)
    {
        if (webUrls?.Any() != true)
            return Task.CompletedTask;
            
        return Task.WhenAll(
            webUrls.Select(url => 
                memory.ImportWebPageAsync(url, cancellationToken: cancellationToken))
        );
    }
    
    private Task ImportMemoryItems(IKernelMemory memory, 
        List<string>? memoryItems,
        CancellationToken cancellationToken)
    {
        if (memoryItems?.Count == 0)
            return Task.CompletedTask;
            
        return Task.WhenAll(
            memoryItems!.Select((item, index) => 
                memory.ImportTextAsync(
                    item, 
                    $"ANSWER_MEMORY_{index + 1}-{memoryItems!.Count}", 
                    cancellationToken: cancellationToken))
        );
    }

}