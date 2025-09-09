using LLamaSharp.KernelMemory;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;

namespace MaIN.Services.Services.LLMService.Memory;

public interface IMemoryService
{
    Task ImportDataToMemory((IKernelMemory km, ITextEmbeddingGenerator? generator) memory,
        ChatMemoryOptions options,
        CancellationToken cancellationToken);
    
    string CleanResponseText(string text);
}