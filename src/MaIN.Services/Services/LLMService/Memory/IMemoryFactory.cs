using LLama;
using LLamaSharp.KernelMemory;
using MaIN.Domain.Entities;
using Microsoft.KernelMemory;

namespace MaIN.Services.Services.LLMService.Memory;

public interface IMemoryFactory
{
    (IKernelMemory KM, LLamaContext TextGenerationContext, LLamaSharpTextEmbeddingGenerator EmbeddingGenerator)
        CreateMemoryWithModel(string modelsPath, LLamaWeights llmModel, string modelName,
            MemoryParams memoryParams);
    IKernelMemory CreateMemoryWithOpenAi(string openAiKey, MemoryParams memoryParams);
    IKernelMemory CreateMemoryWithGemini(string geminiKey, MemoryParams memoryParams);
}