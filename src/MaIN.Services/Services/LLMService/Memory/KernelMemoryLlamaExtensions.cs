using LLama;
using LLama.Common;
using LLama.Sampling;
using LLamaSharp.KernelMemory;
using MaIN.Domain.Entities;
using MaIN.Services.Services.LLMService.Utils;
using MaIN.Services.Utils;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using InferenceParams = LLama.Common.InferenceParams;

namespace MaIN.Services.Services.LLMService.Memory;

public static class KernelMemoryLlamaExtensions 
{
    public static IKernelMemoryBuilder WithLLamaSharpTextGeneration(
        this IKernelMemoryBuilder builder,
        LLamaWeights model, 
        ModelParams memoryParams)
    {
        var context = model.CreateContext(memoryParams);
        var executor = new StatelessExecutor(model, memoryParams);
        
        var inferenceParams = new InferenceParams 
        { 
            AntiPrompts = ["INFO", "<|im_end|>", "Question:"] 
        };

        builder.WithLLamaSharpTextGeneration(
            new LLamaSharp.KernelMemory.LlamaSharpTextGenerator(
                model,
                context,
                executor,
                inferenceParams
            )
        );
        
        return builder;
    }
}