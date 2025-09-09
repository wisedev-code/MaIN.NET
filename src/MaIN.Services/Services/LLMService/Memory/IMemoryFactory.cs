using System.Diagnostics.CodeAnalysis;
using LLama;
using LLamaSharp.KernelMemory;
using MaIN.Domain.Entities;
using Microsoft.KernelMemory;

namespace MaIN.Services.Services.LLMService.Memory;

public interface IMemoryFactory
{
    [Experimental("KMEXP00")]
    (IKernelMemory km, LLamaSharpTextEmbeddingMaINClone generator, LlamaSharpTextGen textGenerator)
        CreateMemoryWithModel(string modelsPath, LLamaWeights llmModel, string modelName,
            MemoryParams memoryParams);
    IKernelMemory CreateMemoryWithOpenAi(string openAiKey, MemoryParams memoryParams);
    IKernelMemory CreateMemoryWithGemini(string geminiKey, MemoryParams memoryParams);
}