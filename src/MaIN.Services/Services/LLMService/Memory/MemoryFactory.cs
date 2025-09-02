using System.Diagnostics.CodeAnalysis;
using LLama;
using LLama.Common;
using LLamaSharp.KernelMemory;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using InferenceParams = LLama.Common.InferenceParams;

namespace MaIN.Services.Services.LLMService.Memory;

public class MemoryFactory() : IMemoryFactory
{
    [Experimental("KMEXP00")]
    public (IKernelMemory km, LLamaSharpTextEmbeddingOwn generator, LlamaSharpTextGen textGenerator)
        CreateMemoryWithModel(string modelsPath,
            LLamaWeights model,
            string modelName,
            MemoryParams memoryParams)
    {
        var path = ResolvePath(modelsPath);
        var embeddingModel = KnownModels.GetEmbeddingModel();
        var embeddingModelPath = Path.Combine(path, embeddingModel.FileName);
        var modelPath = Path.Combine(path, modelName);
        var generator = ConfigureGeneratorOptions(embeddingModelPath, modelPath, memoryParams);
        var searchOptions = ConfigureSearchOptions(memoryParams);
        var parsingOptions = ConfigureParsingOptions();
        var modelParams = new ModelParams(modelPath)
        {
            ContextSize = (uint)memoryParams.ContextSize,
            GpuLayerCount = memoryParams.GpuLayerCount,
        };
        
        //TRY KM integration instead
        var km = new KernelMemoryBuilder()
            .WithLLamaSharpTextGeneration(model, modelParams, memoryParams, out var textGen)
            .WithLLamaSharpTextEmbeddingOwnGeneration(generator)
            .WithSearchClientConfig(searchOptions)
            .WithCustomImageOcr(new OcrWrapper())
            .With(parsingOptions)
            .Build();
        return (km, generator, textGen);
    }

    public IKernelMemory CreateMemoryWithOpenAi(string openAiKey, MemoryParams memoryParams)
    {
        var searchOptions = ConfigureSearchOptions(memoryParams);

        var kernelMemory = new KernelMemoryBuilder()
            .WithSearchClientConfig(searchOptions)
            .WithOpenAIDefaults(openAiKey)
            .Build();

        return kernelMemory;
    }

    public IKernelMemory CreateMemoryWithGemini(string geminiKey, MemoryParams memoryParams)
    {
        var searchOptions = ConfigureSearchOptions(memoryParams);

        var kernelMemory = new KernelMemoryBuilder()
            .WithSearchClientConfig(searchOptions)
#pragma warning disable SKEXP0070 // For evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            .WithSemanticKernelTextGenerationService(
                new GeminiTextGeneratorAdapter(new GoogleAIGeminiChatCompletionService("gemini-2.0-flash", geminiKey)),
                new SemanticKernelConfig())
            .WithSemanticKernelTextEmbeddingGenerationService(
                new GoogleAITextEmbeddingGenerationService("embedding-001", geminiKey), new SemanticKernelConfig())
#pragma warning restore SKEXP0070
            .WithSimpleVectorDb()
            .Build();

        return kernelMemory;
    }

    #region Private Configuration Methods

    private string ResolvePath(string modelsPath)
    {
        var path = modelsPath;

        if (string.IsNullOrEmpty(path))
        {
            throw new InvalidOperationException("Models path not found");
        }

        return path;
    }

    private static LLamaSharpTextEmbeddingOwn ConfigureGeneratorOptions(string embeddingModelPath,
        string modelPath, MemoryParams memoryParams)
    {
        var inferenceParams = new InferenceParams
        {
            AntiPrompts = ["INFO", "<|im_end|>", "Question:"]
        };

        var desiredPath = memoryParams.MultiModalMode ? modelPath : embeddingModelPath;
        var config = new LLamaSharpConfig(desiredPath)
        {
            DefaultInferenceParams = inferenceParams,
            GpuLayerCount = memoryParams.GpuLayerCount,
            ContextSize = (uint?)memoryParams.ContextSize,
        };

        var parameters = new ModelParams(config.ModelPath)
        {
            ContextSize = new uint?(config.ContextSize.GetValueOrDefault(2048U)),
            GpuLayerCount = config.GpuLayerCount.GetValueOrDefault(20),
        };

        var weights = LLamaWeights.LoadFromFile(parameters);
        return new LLamaSharpTextEmbeddingOwn(config, weights);
    }

    private static SearchClientConfig ConfigureSearchOptions(MemoryParams memoryParams)
    {
        return new SearchClientConfig
        {
            MaxMatchesCount = memoryParams.MaxMatchesCount,
            FrequencyPenalty = memoryParams.FrequencyPenalty,
            Temperature = memoryParams.Temperature,
            AnswerTokens = memoryParams.AnswerTokens,
        };
    }

    private static TextPartitioningOptions ConfigureParsingOptions()
    {
        return new TextPartitioningOptions
        {
            MaxTokensPerParagraph = 2048,
            OverlappingTokens = 30,
        };
    }

    #endregion
}