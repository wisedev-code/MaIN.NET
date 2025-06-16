using LLama;
using LLama.Common;
using LLama.Sampling;
using LLamaSharp.KernelMemory;
using MaIN.Domain.Entities;
using Microsoft.KernelMemory;
using InferenceParams = LLama.Common.InferenceParams;

namespace MaIN.Services.Services.LLMService.Memory;

public static class KernelMemoryLlamaExtensions 
{
    public static IKernelMemoryBuilder WithLLamaSharpTextGeneration(
        this IKernelMemoryBuilder builder,
        LLamaWeights model, 
        ModelParams modelParams,
        MemoryParams memoryParams,
        out LLamaContext context)
    {
        context = model.CreateContext(modelParams);
        var executor = new StatelessExecutor(model, modelParams);
        
        var inferenceParams = new InferenceParams 
        { 
            AntiPrompts = ["INFO", "<|im_end|>", "Question:"],
            SamplingPipeline = new DefaultSamplingPipeline()
            {
                Grammar = memoryParams.Grammar != null ? new Grammar(memoryParams.Grammar, "root") : null
            }
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