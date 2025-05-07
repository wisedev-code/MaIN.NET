using LLama;
using LLama.Common;
using LLama.Sampling;
using LLamaSharp.KernelMemory;
using Microsoft.KernelMemory;
using InferenceParams = LLama.Common.InferenceParams;

namespace MaIN.Services.Services.LLMService.Memory;

public static class KernelMemoryLlamaExtensions 
{
    public static IKernelMemoryBuilder WithLLamaSharpTextGeneration(
        this IKernelMemoryBuilder builder,
        LLamaWeights model, 
        ModelParams memoryParams,
        out LLamaContext context)
    {
        context = model.CreateContext(memoryParams);
        var executor = new StatelessExecutor(model, memoryParams);
        
        var inferenceParams = new InferenceParams 
        { 
            AntiPrompts = ["INFO", "<|im_end|>", "Question:"] 
        };

        builder.WithLLamaSharpTextGeneration(
            new LlamaSharpTextGenerator(
                model,
                context,
                executor,
                inferenceParams
            )
        );
        
        return builder;
    }
}