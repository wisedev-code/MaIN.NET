using Microsoft.KernelMemory;

namespace MaIN.Services.Services.LLMService.Memory;

public interface IMemoryService
{
    Task ImportDataToMemory(
        IKernelMemory memory, 
        ChatMemoryOptions options, 
        CancellationToken cancellationToken);
    
    Task<string> ProcessMemoryRequest(
        string modelName, 
        string query, 
        ChatMemoryOptions options, 
        CancellationToken cancellationToken);
    
    string CleanResponseText(string text);
}