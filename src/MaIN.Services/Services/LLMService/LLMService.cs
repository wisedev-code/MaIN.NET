using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using LLama;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;
using LLama.Transformers;
using LLamaSharp.KernelMemory;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using MaIN.Services.Models;
using MaIN.Services.Models.Ollama;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Utils;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Configuration;

namespace MaIN.Services.Services.LLMService;

public class LLMService(IOptions<MaINSettings> options, INotificationService notificationService) : ILLMService
{
    private static readonly ConcurrentDictionary<string, LLamaWeights> modelCache = new();
    private static readonly ConcurrentDictionary<string, ChatSession> sessionCache = new(); // Cache for chat sessions

    public async Task<ChatResult?> Send(Chat? chat, bool interactiveUpdates = false, bool newSession = false)
    {
        if (chat == null || chat.Messages == null || !chat.Messages.Any())
            return null;

        if (chat.Model == KnownModelNames.Llava_7b) //TODO include better support for vision models
        {
            return await HandleImageInterpreter(chat)!;
        }

        var path = options.Value.ModelsPath;
        var model = KnownModels.GetModel(path, chat.Model);
        var modelKey = model.FileName;

        // Get or load the model asynchronously.
        var llmModel = await GetOrLoadModelAsync(path, modelKey);
        var inferenceParams = new InferenceParams
        {
            SamplingPipeline = new DefaultSamplingPipeline
            {
                Temperature = 0.6f
            },
            MaxTokens = 1024,
            AntiPrompts = new[] { llmModel.Tokens.EndOfTurnToken ?? "User:" }
        };

        var parameters = new ModelParams(Path.Combine(path, modelKey))
        {
            ContextSize = 1024,
            GpuLayerCount = 30,
        };

        var session = newSession ? GetOrCreateSession(chat.Id, () =>
        {
            var context = llmModel.CreateContext(parameters);
            var history = new ChatHistory();
            var executor = new InteractiveExecutor(context);
            return new ChatSession(executor, history);
        }) : new ChatSession(new InteractiveExecutor(llmModel.CreateContext(parameters)));

        // Add all messages to the session history.
        AddMessagesToHistory(session, chat.Messages);

        // Generate a response.
        session.WithHistoryTransform(new PromptTemplateTransformer(llmModel, withAssistant: true));
        session.WithOutputTransform(new LLamaTransforms.KeywordTextOutputStreamTransform(
            new[] { llmModel.Tokens.EndOfTurnToken ?? "User:", "�" },
            redundancyLength: 5));

        var resultBuilder = new StringBuilder();

        await foreach (var text in session.ChatAsync(
                           new ChatHistory.Message(AuthorRole.User, chat.Messages.Last().Content),
                           inferenceParams))
        {
            if (interactiveUpdates)
            {
                await notificationService.DispatchNotification(
                    NotificationMessageBuilder.CreateChatCompletion(
                        chat.Id,
                        text,
                        false),
                    "ReceiveMessageUpdate");
            }
            resultBuilder.Append(text);
            Console.Write(text);
        }

        if (interactiveUpdates)
        {
            await notificationService.DispatchNotification( NotificationMessageBuilder.CreateChatCompletion(
                chat.Id,
                resultBuilder.ToString(),
                true), "ReceiveMessageUpdate");
        }
        
        var chatResult = new ChatResult
        {
            Done = true,
            CreatedAt = DateTime.Now,
            Model = chat.Model,
            Message = new MessageDto
            {
                Content = resultBuilder.ToString(),
                Role = AuthorRole.Assistant.ToString()
            }
        };

        return chatResult;
    }

    private async Task<ChatResult>? HandleImageInterpreter(Chat chat)
    {
        var path = options.Value.ModelsPath;
        var modelConfig = KnownModels.GetModel(path, chat.Model);
        var modelKey = modelConfig.FileName;

        var parameters = new ModelParams(Path.Combine(path, modelKey));

        using var model = LLamaWeights.LoadFromFile(parameters);
        using var context = model.CreateContext(parameters);

        // Llava Init
        var inferenceParams = new InferenceParams() { AntiPrompts = new[] { model.Tokens.EndOfTurnToken ?? "User:" }};
        var ex = new InteractiveExecutor(context);
        ex.Context.NativeHandle.KvCacheRemove( LLamaSeqId.Zero, -1, -1 );
        ex.Images.Add(chat.Messages!.Last().Images);
        var result = new StringBuilder();
        await foreach (var text in ex.InferAsync(chat.Messages!.Last().Content, inferenceParams))
        {
            Console.Write(text);
            result.Append(text);
        }
        
        var chatResult = new ChatResult
        {
            Done = true,
            CreatedAt = DateTime.Now,
            Model = chat.Model,
            Message = new MessageDto
            {
                Content = result.ToString(),
                Role = AuthorRole.Assistant.ToString()
            }
        };

        return chatResult;
    }

    // Caching session logic.
    private ChatSession GetOrCreateSession(string chatId, Func<ChatSession> createSession)
    {
        if (!sessionCache.TryGetValue(chatId, out var session))
        {
            session = createSession();
            sessionCache[chatId] = session;
        }

        return session;
    }

// Simplified message addition.
    private void AddMessagesToHistory(ChatSession session, List<Message> messages)
    {
        var existingMessages = session.History.Messages
            .Select(m => new { m.AuthorRole, m.Content })
            .ToHashSet();

        foreach (var message in messages.SkipLast(1))
        {
            var messageKey = new { AuthorRole = Enum.Parse<AuthorRole>(message.Role), message.Content };

            if (!existingMessages.Contains(messageKey))
            {
                session.History.AddMessage(messageKey.AuthorRole, message.Content);
            }
        }
    }

    [Experimental("SKEXP0001")]
    public async Task<ChatResult?> AskMemory(Chat? chat, List<string>? jsons = null,
        string? filePath = null, List<string>? memory = null)
    {
        var path = options.Value.ModelsPath;
        var model = KnownModels.GetModel(path, chat!.Model);
        var modelKey = model.FileName;

        var kernelMemory = CreateMemory(modelKey, path);

        if (jsons != null)
        {
            for (var index = 0; index < jsons.Count; index++)
            {
                await kernelMemory.ImportTextAsync(jsons[index], $"JSON_CHUNK_{index + 1}-{jsons.Count}");

            }
        }

        if (memory != null)
        {
            for (var index = 0; index < memory.Count; index++)
            {
                await kernelMemory.ImportTextAsync(memory[index], $"ANSWER_MEMORY_{index + 1}-{memory.Count}");
            }
        }

        var userMsg = chat.Messages!.Last();
        var result = await kernelMemory.AskAsync(userMsg.Content);

        await kernelMemory.DeleteIndexAsync();
        
        var chatResult = new ChatResult()
        {
            Done = true,
            CreatedAt = DateTime.Now,
            Model = chat.Model,
            Message = new MessageDto()
            {
                Content = result.Result,
                Role = AuthorRole.Assistant.ToString()
            }
        };

        return chatResult;
    }


    [Experimental("KMEXP01")]
    private static IKernelMemory CreateMemory(string modelName, string path)
    {
        InferenceParams infParams = new() { AntiPrompts = ["INFO"] };

        LLamaSharpConfig lsConfig = new(Path.Combine(path, KnownModels.GetEmbeddingModel().FileName))
            { DefaultInferenceParams = infParams };

        SearchClientConfig searchClientConfig = new()
        {
            MaxMatchesCount = 5,
            AnswerTokens = 500,
        };

        TextPartitioningOptions parseOptions = new()
        {
            MaxTokensPerParagraph = 300,
            MaxTokensPerLine = 100,
        };

        return new KernelMemoryBuilder()
            .WithLLamaSharpMaINTemp(lsConfig, Path.Combine(path, modelName))
            .WithSearchClientConfig(searchClientConfig)
            .With(parseOptions)
            .Build();
    }

    private async Task<LLamaWeights> GetOrLoadModelAsync(string path, string modelKey)
    {
        if (modelCache.TryGetValue(modelKey, out var cachedModel))
        {
            return cachedModel;
        }

        var parameters = new ModelParams(Path.Combine(path, modelKey));
        var loadedModel = await LLamaWeights.LoadFromFileAsync(parameters);
        return modelCache.GetOrAdd(modelKey, loadedModel);
    }

    public Task<List<string>> GetCurrentModels()
    {
        var path = options.Value.ModelsPath;
        var files = Directory.GetFiles(path, "*.gguf", SearchOption.AllDirectories).ToList();
        var models = new List<string>();
        foreach (var file in files)
        {
            var model = KnownModels.GetModelByFileName(path, Path.GetFileName(file));
            if (model != null)
            {
                models.Add(model.Value.Name);
            }
        }

        return Task.FromResult(models);
    }

    public Task CleanSessionCache(string id)
    {
        sessionCache.Remove(id, out var session);
        session?.Executor.Context.NativeHandle.KvCacheClear();
        session?.Executor.Context.Dispose();
        return Task.CompletedTask;
    }
}

file static class KernelMemFix
{
    private static readonly ConcurrentDictionary<string, LLamaWeights> ModelCache = new();

    [Experimental("KMEXP01")]
    public static IKernelMemoryBuilder WithLLamaSharpMaINTemp(this IKernelMemoryBuilder builder,
        LLamaSharpConfig config, string modelPath)
    {
        // Create ModelParams for the first model.
        var parameters1 = new ModelParams(modelPath)
        {
            ContextSize = 1024,
            GpuLayerCount = 55,
        };

        // Load the first model with caching.
        var model = GetOrLoadModel(parameters1);

        // Create ModelParams for the second model.
        ModelParams parameters2 = new ModelParams(config.ModelPath)
        {
            ContextSize = new uint?(config.ContextSize.GetValueOrDefault(2048U)),
            GpuLayerCount = config.GpuLayerCount.GetValueOrDefault(20),
            Embeddings = false,
            MainGpu = config.MainGpu,
            SplitMode = config.SplitMode,
        };

        // Load the second model with caching.
        var weights = GetOrLoadModel(parameters2);

        var context = model.CreateContext(parameters2);

        StatelessExecutor executor = new StatelessExecutor(model, parameters2);
        builder.WithLLamaSharpTextEmbeddingGeneration(new LLamaSharpTextEmbeddingGenerator(config, weights));
        builder.WithLLamaSharpTextGeneration(new LlamaSharpTextGenerator(model, context, executor,
            config.DefaultInferenceParams));
        return builder;
    }

    private static LLamaWeights GetOrLoadModel(ModelParams modelParams)
    {
        // Use a unique key based on the serialized ModelParams object.
        string cacheKey = GenerateCacheKey(modelParams);

        // Retrieve from cache or load if not already cached.
        return ModelCache.GetOrAdd(cacheKey, _ => LLamaWeights.LoadFromFile(modelParams));
    }

    private static string GenerateCacheKey(ModelParams modelParams)
    {
        // Create a unique key by combining important properties of ModelParams.
        return $"{modelParams.ModelPath}:{modelParams.ContextSize}:{modelParams.GpuLayerCount}:{modelParams.MainGpu}:{modelParams.SplitMode}";
    }
}