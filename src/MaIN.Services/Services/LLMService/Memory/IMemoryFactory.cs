using LLama;
using LLama.Common;
using MaIN.Domain.Entities;
using Microsoft.KernelMemory;

namespace MaIN.Services.Services.LLMService.Memory;

public interface IMemoryFactory
{
    IKernelMemory CreateMemory(string modelsPath, string modelName);
    (IKernelMemory KM, LLamaContext TextGenerationContext) CreateMemoryWithModel(string modelsPath, LLamaWeights llmModel,
        MemoryParams memoryParams);
    IKernelMemory CreateMemoryWithOpenAi(string openAiKey, MemoryParams memoryParams);

}