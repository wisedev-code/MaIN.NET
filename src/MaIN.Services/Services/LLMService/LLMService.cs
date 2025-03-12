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
using MaIN.Services.Services.Abstract;
using MaIN.Services.Utils;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Configuration;
using InferenceParams = LLama.Common.InferenceParams;

namespace MaIN.Services.Services.LLMService;

public class LLMService(MaINSettings options, INotificationService notificationService) : ILLMService
{
    private const string DefaultModelEnvPath = "MaIN_ModelsPath";
    private static readonly ConcurrentDictionary<string, LLamaWeights> modelCache = new();
    private static readonly ConcurrentDictionary<string, ChatSession> sessionCache = new(); // Cache for chat sessions

    public async Task<ChatResult?> Send(
        Chat? chat, 
        bool interactiveUpdates = false,
        bool newSession = false,
        Func<string?, Task>? changeOfValue = null)
    {
        if (chat == null || !chat.Messages.Any())
            return null;

        if (chat.Model == KnownModelNames.Llava_7b) //TODO include better support for vision models
        {
            return await HandleImageInterpreter(chat)!;
        }

        var path = options.ModelsPath ?? Environment.GetEnvironmentVariable(DefaultModelEnvPath);
        var model = KnownModels.GetModel(path, chat.Model);
        var modelKey = model.FileName;

        // Get or load the model asynchronously.
        var llmModel = await GetOrLoadModelAsync(path, modelKey);
        var inferenceParams = new InferenceParams
        {
            SamplingPipeline = new DefaultSamplingPipeline
            {
                Temperature = chat.InterferenceParams.Temperature
            },
            MaxTokens = chat.InterferenceParams.ContextSize,
            AntiPrompts = new[] { llmModel.Vocab.EOT?.ToString() ?? "User:" }
        };

        var parameters = new ModelParams(Path.Combine(path, modelKey))
        {
            ContextSize = (uint?)chat.InterferenceParams.ContextSize,
            GpuLayerCount = 30,
        };

        var session = newSession
            ? GetOrCreateSession(chat.Id, () =>
            {
                var context = llmModel.CreateContext(parameters);
                var history = new ChatHistory();
                var executor = new InteractiveExecutor(context);
                return new ChatSession(executor, history);
            })
            : new ChatSession(new InteractiveExecutor(llmModel.CreateContext(parameters)));

        // Add all messages to the session history.
        AddMessagesToHistory(session, chat.Messages);

        // Generate a response.
        session.WithHistoryTransform(new PromptTemplateTransformer(llmModel, withAssistant: true));
        session.WithOutputTransform(new LLamaTransforms.KeywordTextOutputStreamTransform(
            new[] { llmModel.Vocab.EOT.ToString() ?? "User:", "�" },
            redundancyLength: 5));

        var resultBuilder = new StringBuilder();

        var lastMessage = chat.Messages.Last();

        if (lastMessage.Files?.Any() ?? false)
        {
#pragma warning disable SKEXP0001
            var textData = lastMessage.Files.Where(x => x.Content is not null)
                .ToDictionary(x => x.Name, x => x.Content);
            var fileData =
                lastMessage.Files.Where(x => x.Path is not null)
                    .ToDictionary(x => x.Name, x => x.Path); //shity coode TODO
            var result = await AskMemory(chat, textData!, fileData!);
            resultBuilder.Append(result!.Message.Content);
#pragma warning restore SKEXP0001
        }
        else
        {
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

                changeOfValue?.Invoke(text);
                resultBuilder.Append(text);
            }
        }

        if (interactiveUpdates)
        {
            await notificationService.DispatchNotification(NotificationMessageBuilder.CreateChatCompletion(
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

    private async Task<ChatResult> HandleImageInterpreter(Chat? chat)
    {
        var path = options.ModelsPath ?? Environment.GetEnvironmentVariable(DefaultModelEnvPath);
        var modelConfig = KnownModels.GetModel(path, chat.Model);
        var modelKey = modelConfig.FileName;

        var parameters = new ModelParams(Path.Combine(path, modelKey));

        using var model = LLamaWeights.LoadFromFile(parameters);
        using var context = model.CreateContext(parameters);

        // Llava Init
        var inferenceParams = new InferenceParams() { AntiPrompts = new[] { model.Vocab.EOT.ToString() ?? "User:" } };
        var ex = new InteractiveExecutor(context);
        ex.Context.NativeHandle.KvCacheRemove(LLamaSeqId.Zero, -1, -1);
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

    private ChatSession GetOrCreateSession(string chatId, Func<ChatSession> createSession)
    {
        if (!sessionCache.TryGetValue(chatId, out var session))
        {
            session = createSession();
            sessionCache[chatId] = session;
        }

        return session;
    }

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
    public async Task<ChatResult?> AskMemory(Chat? chat,
        Dictionary<string, string>? textData = null,
        Dictionary<string, string>? fileData = null,
        List<string>? webUrls = null,
        List<string>? memory = null)
    {
        var path = options.ModelsPath ?? Environment.GetEnvironmentVariable(DefaultModelEnvPath);
        var model = KnownModels.GetModel(path, chat!.Model);
        var modelKey = model.FileName;

        var kernelMemory = CreateMemory(modelKey, path, out var generator);

        if (textData != null)
        {
            foreach (var item in textData)
            {
                await kernelMemory.ImportTextAsync(item.Value, item.Key);
            }
        }

        if (fileData != null)
        {
            foreach (var item in fileData)
            {
                await kernelMemory.ImportDocumentAsync(item.Value, item.Key);
            }
        }

        if (webUrls != null)
        {
            foreach (var url in webUrls)
            {
                await kernelMemory.ImportWebPageAsync(url);
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
                Content = result.Result.Replace("Question:", string.Empty).Replace("Assistant:", string.Empty),
                Role = AuthorRole.Assistant.ToString()
            }
        };

        generator.Dispose();

        return chatResult;
    }


    [Experimental("KMEXP01")]
    private static IKernelMemory CreateMemory(string modelName, string? path,
        out KernelMemFix.LlamaSharpTextGenerator generator)
    {
        InferenceParams infParams = new() { AntiPrompts = ["INFO", "<|im_end|>", "Question:"] };

        LLamaSharpConfig lsConfig = new(Path.Combine(path, KnownModels.GetEmbeddingModel().FileName))
            { DefaultInferenceParams = infParams };

        SearchClientConfig searchClientConfig = new()
        {
            MaxMatchesCount = 5,
            FrequencyPenalty = 1,
            Temperature = 0.6f,
            AnswerTokens = 500,
        };

        TextPartitioningOptions parseOptions = new()
        {
            MaxTokensPerParagraph = 300,
            MaxTokensPerLine = 100,
        };

        return new KernelMemoryBuilder()
            //.WithLLamaSharpDefaults2(lsConfig)
            .WithLLamaSharpMaINTemp(lsConfig, path, modelName, out generator)
            .WithSearchClientConfig(searchClientConfig)
            .WithCustomImageOcr(new OcrWrapper())
            .With(parseOptions)
            .Build();
    }

    internal static async Task<LLamaWeights> GetOrLoadModelAsync(string? path, string modelKey)
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
        var path = options.ModelsPath ?? Environment.GetEnvironmentVariable(DefaultModelEnvPath);
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

internal static class KernelMemFix
{
    [Experimental("KMEXP00")]
    public sealed class LlamaSharpTextGenerator : ITextGenerator, ITextTokenizer, IDisposable
    {
        private readonly StatelessExecutor _executor;
        private readonly LLamaWeights _weights;
        private readonly bool _ownsWeights;
        private readonly LLamaContext _context;
        private readonly bool _ownsContext;
        private readonly InferenceParams? _defaultInferenceParams;

        public int MaxTokenTotal { get; }


        public LlamaSharpTextGenerator(
            LLamaWeights weights,
            LLamaContext context,
            StatelessExecutor? executor = null,
            InferenceParams? inferenceParams = null)
        {
            this._weights = weights;
            this._context = context;
            this._executor = executor ?? new StatelessExecutor(this._weights, this._context.Params);
            this._defaultInferenceParams = inferenceParams;
            this.MaxTokenTotal = (int)this._context.ContextSize;
        }

        public void Dispose()
        {
            if (this._ownsWeights)
                this._weights.Dispose();
            if (!this._ownsContext)
                return;
            this._context.Dispose();
        }

        public IAsyncEnumerable<GeneratedTextContent> GenerateTextAsync(string prompt, TextGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            return _executor
                .InferAsync(prompt, OptionsToParams(options, _defaultInferenceParams),
                    cancellationToken: cancellationToken)
                .Select(a => new GeneratedTextContent(a));
        }

        private static InferenceParams OptionsToParams(
            TextGenerationOptions options,
            InferenceParams? defaultParams)
        {
            if (defaultParams != (InferenceParams)null)
                return defaultParams with
                {
                    AntiPrompts = (IReadOnlyList<string>)defaultParams.AntiPrompts
                        .Concat<string>((IEnumerable<string>)options.StopSequences).ToList<string>().AsReadOnly(),
                    MaxTokens = options.MaxTokens ?? defaultParams.MaxTokens,
                    SamplingPipeline = (ISamplingPipeline)new DefaultSamplingPipeline()
                    {
                        Temperature = (float)options.Temperature,
                        FrequencyPenalty = (float)options.FrequencyPenalty,
                        PresencePenalty = (float)options.PresencePenalty,
                        TopP = (float)options.NucleusSampling
                    }
                };
            return new InferenceParams()
            {
                AntiPrompts = (IReadOnlyList<string>)options.StopSequences.ToList<string>().AsReadOnly(),
                MaxTokens = options.MaxTokens.GetValueOrDefault(1024),
                SamplingPipeline = (ISamplingPipeline)new DefaultSamplingPipeline()
                {
                    Temperature = (float)options.Temperature,
                    FrequencyPenalty = (float)options.FrequencyPenalty,
                    PresencePenalty = (float)options.PresencePenalty,
                    TopP = (float)options.NucleusSampling
                }
            };
        }

        public int CountTokens(string text) => this._context.Tokenize(text, special: true).Length;

        public IReadOnlyList<string> GetTokens(string text)
        {
            LLamaToken[] source = this._context.Tokenize(text, special: true);
            StreamingTokenDecoder decoder = new StreamingTokenDecoder(this._context);
            Func<LLamaToken, string> selector = (Func<LLamaToken, string>)(x =>
            {
                decoder.Add(x);
                return decoder.Read();
            });
            return (IReadOnlyList<string>)((IEnumerable<LLamaToken>)source).Select<LLamaToken, string>(selector)
                .ToList<string>();
        }
    }

    [Experimental("KMEXP00")]
    public static IKernelMemoryBuilder WithLLamaSharpTextGeneration(
        this IKernelMemoryBuilder builder,
        LlamaSharpTextGenerator textGenerator)
    {
        builder.AddSingleton((ITextGenerator)textGenerator);
        return builder;
    }

    public static LLamaWeights? Weights = null;

    [Experimental("KMEXP01")]
    public static IKernelMemoryBuilder WithLLamaSharpMaINTemp(this IKernelMemoryBuilder builder,
        LLamaSharpConfig config, string? path, string modelName, out LlamaSharpTextGenerator generator)
    {
        // Load the first model with caching.
        var model = LLMService.GetOrLoadModelAsync(path, modelName).Result;

        // Create ModelParams for the second model.
        ModelParams parameters2 = new ModelParams(config.ModelPath)
        {
            ContextSize = new uint?(config.ContextSize.GetValueOrDefault(2048U)),
            GpuLayerCount = config.GpuLayerCount.GetValueOrDefault(20),
            //Embeddings = true,
            //MainGpu = config.MainGpu,
            //SplitMode = new GPUSplitMode?(config.SplitMode)
        };

        Weights ??= LLamaWeights.LoadFromFile(parameters2);

        var context = model.CreateContext(parameters2);
        StatelessExecutor executor = new StatelessExecutor(model, parameters2);

        generator = new LlamaSharpTextGenerator(model, context, executor,
            config.DefaultInferenceParams);

        builder.WithLLamaSharpTextEmbeddingGeneration(new LLamaSharpTextEmbeddingGenerator(config, Weights));
        builder.WithLLamaSharpTextGeneration(generator);
        return builder;
    }
}