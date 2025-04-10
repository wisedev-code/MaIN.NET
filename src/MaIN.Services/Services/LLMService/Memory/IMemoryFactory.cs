using MaIN.Domain.Entities;
using Microsoft.KernelMemory;

namespace MaIN.Services.Services.LLMService.Memory;

public interface IMemoryFactory
{
    IKernelMemory CreateMemory(string? modelsPath, string modelName);
    IKernelMemory CreateMemoryWithParams(string? modelsPath, string modelName, MemoryParams memoryParams);
}