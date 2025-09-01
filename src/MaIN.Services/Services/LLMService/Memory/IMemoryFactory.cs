using LLama;
using LLamaSharp.KernelMemory;
using MaIN.Domain.Entities;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel.Memory;

namespace MaIN.Services.Services.LLMService.Memory;

public interface IMemoryFactory
{
    ISemanticTextMemory
        CreateMemoryWithModel(string modelsPath, LLamaWeights llmModel, string modelName,
            MemoryParams memoryParams);
    IKernelMemory CreateMemoryWithOpenAi(string openAiKey, MemoryParams memoryParams);
    IKernelMemory CreateMemoryWithGemini(string geminiKey, MemoryParams memoryParams);
}