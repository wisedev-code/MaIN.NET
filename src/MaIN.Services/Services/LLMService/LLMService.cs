using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using LLama;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;
using LLama.Transformers;
using LLamaSharp.KernelMemory;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Models;
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

    public async Task<ChatResult?> Send(Chat chat,
        bool interactiveUpdates = false,
        bool newSession = false,
        Func<LLMTokenValue, Task>? changeOfValue = null)
    {
        if (!chat.Messages.Any())
            return null;

        var path = options.ModelsPath ?? Environment.GetEnvironmentVariable(DefaultModelEnvPath);
        if (path == null)
        {
            throw new Exception("ModelsPath setting is not present in configuration");
        }
        
        var model = KnownModels.GetModel(path, chat.Model);
        var thinkingState = new ThinkingState();
        var modelKey = model.FileName;

        var llmModel = await GetOrLoadModelAsync(path!, modelKey);
        var inferenceParams = new InferenceParams
        {
            SamplingPipeline = new DefaultSamplingPipeline
            {
                Temperature = chat.InterferenceParams.Temperature,
                TopK = chat.InterferenceParams.TopK,
                TopP = chat.InterferenceParams.TopP
            },
            AntiPrompts = [llmModel.Vocab.EOT?.ToString() ?? "User:"],
            TokensKeep = chat.InterferenceParams.TokensKeep,
            MaxTokens = chat.InterferenceParams.MaxTokens
        };

        var parameters = new ModelParams(Path.Combine(path, modelKey))
        {
            ContextSize = (uint?)chat.InterferenceParams.ContextSize,
            GpuLayerCount = chat.InterferenceParams.GpuLayerCount,
            SeqMax = chat.InterferenceParams.SeqMax,
            BatchSize = chat.InterferenceParams.BatchSize,
            UBatchSize = chat.InterferenceParams.UBatchSize,
            Embeddings = chat.InterferenceParams.Embeddings,
            TypeK = (GGMLType)chat.InterferenceParams.TypeK,
            TypeV = (GGMLType)chat.InterferenceParams.TypeV,
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

        var startSession = session.History.Messages.Count == 0;
        AddMessagesToHistory(session, chat.Messages);

        session.WithHistoryTransform(new PromptTemplateTransformer(llmModel, withAssistant: true));
        session.WithOutputTransform(new LLamaTransforms.KeywordTextOutputStreamTransform(
            new[] { llmModel.Vocab.EOT.ToString() ?? "User:", "�"},
            redundancyLength: 5));

        var listOfTokens = new List<LLMTokenValue>();
        var lastMessage = chat.Messages.Last();
        var finalPrompt = startSession && model.AdditionalPrompt is not null ? $"{lastMessage.Content}{model.AdditionalPrompt}" : lastMessage.Content;

        if (lastMessage.Files?.Any() ?? false)
        {
#pragma warning disable SKEXP0001
            var textData = lastMessage.Files
                .Where(x => x.Content is not null)
                .ToDictionary(x => x.Name, x => x.Content);
            
            var fileData = lastMessage.Files
                .Where(x => x.Path is not null)
                .ToDictionary(x => x.Name, x => x.Path); //shity coode TODO
            
            var streamData = lastMessage.Files
                .Where(x => x.StreamContent is not null)
                .ToDictionary(x => x.Name, x => x.StreamContent);
            
            var result = await AskMemory(chat, textData!, fileData!, streamData!); 
            
            listOfTokens.Add(new LLMTokenValue()
            {
                Type = TokenType.FullAnswer,
                Text = result!.Message.Content
            });
        }
        else
        {
            await foreach (var text in session.ChatAsync(
                               new ChatHistory.Message(AuthorRole.User, finalPrompt),
                               inferenceParams))
            {
                var token = model.ReasonFunction is not null
                    ? model.ReasonFunction(text, thinkingState)
                    : new LLMTokenValue()
                    {
                        Type = TokenType.Message,
                        Text = text
                    };
                
                if (interactiveUpdates)
                {
                    await notificationService.DispatchNotification(
                        NotificationMessageBuilder.CreateChatCompletion(
                            chat.Id,
                            token,
                            false),
                        "ReceiveMessageUpdate");
                }

                changeOfValue?.Invoke(token);
                listOfTokens.Add(token);
            }
        }

        var stringToReturn = string.Concat(listOfTokens.Select(x => x.Text));
        if (interactiveUpdates)
        {
            await notificationService.DispatchNotification(NotificationMessageBuilder.CreateChatCompletion(
                chat.Id,
                new LLMTokenValue()
                {
                    Type = TokenType.FullAnswer,
                    Text = stringToReturn
                },
                true), "ReceiveMessageUpdate");
        }

        var chatResult = new ChatResult
        {
            Done = true,
            CreatedAt = DateTime.Now,
            Model = chat.Model,
            Message = new Message
            {
                Content = stringToReturn,
                Tokens = listOfTokens,
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
    public async Task<ChatResult?> AskMemory(Chat chat,
        Dictionary<string, string>? textData = null,
        Dictionary<string, string>? fileData = null,
        Dictionary<string, FileStream>? streamData = null,
        List<string>? webUrls = null,
        List<string>? memory = null)
    {
        var path = options.ModelsPath ?? Environment.GetEnvironmentVariable(DefaultModelEnvPath);
        if (path == null)
            throw new Exception("ModelsPath setting is not present in configuration"); //TODO good candidate for custom exception
        
        var model = KnownModels.GetModel(path, chat.Model);
        var modelKey = model.FileName;

        var kernelMemory = CreateMemory(modelKey, path, chat.MemoryParams);

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

        if (streamData != null)
        {
            foreach (var item in streamData)
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

        var userMsg = chat.Messages.Last();
        var result = await kernelMemory.AskAsync(userMsg.Content);

        await kernelMemory.DeleteIndexAsync();

        var chatResult = new ChatResult()
        {
            Done = true,
            CreatedAt = DateTime.Now,
            Model = chat.Model,
            Message = new Message
            {
                Content = result.Result.Replace("Question:", string.Empty).Replace("Assistant:", string.Empty),
                Role = AuthorRole.Assistant.ToString()
            }
        };
        
        return chatResult;
    }


    [Experimental("KMEXP01")]
    private static IKernelMemory CreateMemory(string modelName, string path, MemoryParams memoryParams)
    {
        InferenceParams infParams = new() { AntiPrompts = ["INFO", "<|im_end|>", "Question:"] };

        LLamaSharpConfig lsConfig = new(Path.Combine(path, KnownModels.GetEmbeddingModel().FileName))
            { DefaultInferenceParams = infParams };
        
        SearchClientConfig searchClientConfig = new()
        {
            MaxMatchesCount = memoryParams.MaxMatchesCount,
            FrequencyPenalty = memoryParams.FrequencyPenalty,
            Temperature = memoryParams.Temperature,
            AnswerTokens = memoryParams.AnswerTokens,
        };

        TextPartitioningOptions parseOptions = new()
        {
            MaxTokensPerParagraph = 300,
            MaxTokensPerLine = 100,
        };

        return new KernelMemoryBuilder()
            //.WithLLamaSharpDefaults(lsConfig)
            //.WithLLamaSharpDefaults2(lsConfig)
            .WithLLamaSharpMaINTemp(lsConfig, path, modelName, out var generator)
            .WithSearchClientConfig(searchClientConfig)
            .WithCustomImageOcr(new OcrWrapper())
            .With(parseOptions)
            .Build();
    }

    internal static async Task<LLamaWeights> GetOrLoadModelAsync(string path, string modelKey)
    {
        if (modelCache.TryGetValue(modelKey, out var cachedModel))
        {
            return cachedModel;
        }

        var parameters = new ModelParams(Path.Combine(path, modelKey));
        var loadedModel = await LLamaWeights.LoadFromFileAsync(parameters);
        return modelCache.GetOrAdd(modelKey, loadedModel);
    }

    public Task<List<string?>> GetCurrentModels()
    {
        var path = options.ModelsPath ?? Environment.GetEnvironmentVariable(DefaultModelEnvPath); //TODO add handling for null path
        var files = Directory.GetFiles(path!, "*.gguf", SearchOption.AllDirectories).ToList();
        var models = new List<string?>();
        foreach (var file in files)
        {
            var model = KnownModels.GetModelByFileName(path!, Path.GetFileName(file));
            if (model != null)
            {
                models.Add(model.Name);
            }
        }

        return Task.FromResult(models);
    }

    public Task CleanSessionCache(string? id)
    {
        sessionCache!.Remove(id, out var session);
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
        private readonly LLamaContext _context;
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
            this._weights.Dispose();
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
            if (defaultParams != null!)
                return defaultParams with
                {
                    AntiPrompts = defaultParams.AntiPrompts
                        .Concat(options.StopSequences).ToList().AsReadOnly(),
                    MaxTokens = options.MaxTokens ?? defaultParams.MaxTokens,
                    SamplingPipeline = new DefaultSamplingPipeline()
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
        var model = LLMService.GetOrLoadModelAsync(path!, modelName).Result;

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