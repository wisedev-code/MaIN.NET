using System.Collections.Concurrent;
using System.Text;
using LLama;
using LLama.Batched;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService.Memory;
using MaIN.Services.Services.LLMService.Utils;
using MaIN.Services.Services.Models;
using MaIN.Services.Utils;
using Microsoft.KernelMemory;
using InferenceParams = MaIN.Domain.Entities.InferenceParams;

namespace MaIN.Services.Services.LLMService;

public class LLMService : ILLMService
{
    private const string DEFAULT_MODEL_ENV_PATH = "MaIN_ModelsPath";
    private static readonly ConcurrentDictionary<string, ChatSession> _sessionCache = new();

    private readonly MaINSettings options;
    private readonly INotificationService notificationService;
    private readonly IMemoryService memoryService;
    private readonly IMemoryFactory memoryFactory;
    private readonly string modelsPath;

    public LLMService(
        MaINSettings options,
        INotificationService notificationService,
        IMemoryService memoryService,
        IMemoryFactory memoryFactory)
    {
        this.options = options;
        this.notificationService = notificationService;
        this.memoryService = memoryService;
        this.memoryFactory = memoryFactory;
        modelsPath = GetModelsPath();
    }

    public async Task<ChatResult?> Send(
        Chat chat,
        ChatRequestOptions requestOptions,
        CancellationToken cancellationToken = default)
    {
        if (chat.Messages.Count == 0)
            return null;

        var lastMsg = chat.Messages.Last();

        if (ChatHelper.HasFiles(lastMsg))
        {
            var memoryOptions = ChatHelper.ExtractMemoryOptions(lastMsg);
            return await AskMemory(chat, memoryOptions, cancellationToken);
        }

        var model = KnownModels.GetModel(chat.Model);
        var tokens = await ProcessChatRequest(chat, model, lastMsg, requestOptions, cancellationToken);
        lastMsg.MarkProcessed();
        return await CreateChatResult(chat, tokens, requestOptions);
    }

    public Task<string[]> GetCurrentModels()
    {
        var models = Directory.GetFiles(modelsPath, "*.gguf", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .Where(fileName => KnownModels.GetModelByFileName(modelsPath, fileName!) != null)
            .Select(fileName => KnownModels.GetModelByFileName(modelsPath, fileName!)!.Name)
            .ToArray();

        return Task.FromResult(models);
    }

    public Task CleanSessionCache(string? id)
    {
        if (string.IsNullOrEmpty(id) || !_sessionCache.TryRemove(id, out var session))
            return Task.CompletedTask;

        session.Executor.Context.Dispose();
        return Task.CompletedTask;
    }

    public async Task<ChatResult?> AskMemory(
        Chat chat,
        ChatMemoryOptions memoryOptions,
        CancellationToken cancellationToken = default)
    {
        var model = KnownModels.GetModel(chat.Model);
        var parameters = new ModelParams(Path.Combine(modelsPath, model.FileName))
        {
            GpuLayerCount = chat.MemoryParams.GpuLayerCount,
            ContextSize = (uint)chat.MemoryParams.ContextSize,
            Embeddings = true
        };
        var disableCache = chat.Properties.CheckProperty(ServiceConstants.Properties.DisableCacheProperty);
        var llmModel = disableCache
            ? await LLamaWeights.LoadFromFileAsync(parameters, cancellationToken)
            : await ModelLoader.GetOrLoadModelAsync(modelsPath, model.FileName);

        var memory = memoryFactory.CreateMemoryWithModel(
            modelsPath,
            llmModel,
            model.FileName,
            chat.MemoryParams);

        await memoryService.ImportDataToMemory(memory, memoryOptions, cancellationToken);
        var userMessage = chat.Messages.Last();
        var result = await memory.AskAsync(
            userMessage.Content,
            cancellationToken: cancellationToken);
        await memory.DeleteIndexAsync(cancellationToken: cancellationToken);
        
        if (disableCache)
        {
            llmModel.Dispose();
        }

        // memory.TextGenerationContext.Dispose();
        // memory.EmbeddingGenerator.Dispose();

        return new ChatResult
        {
            Done = true,
            CreatedAt = DateTime.Now,
            Model = chat.Model,
            Message = new Message
            {
                Content = memoryService.CleanResponseText(result.Result),
                Role = nameof(AuthorRole.Assistant),
                Type = MessageType.LocalLLM,
            }
        };
    }

    private async Task<List<LLMTokenValue>> ProcessChatRequest(
        Chat chat,
        Model model,
        Message lastMsg,
        ChatRequestOptions requestOptions,
        CancellationToken cancellationToken)
    {
        var modelKey = model.FileName;
        var thinkingState = new ThinkingState();
        var tokens = new List<LLMTokenValue>();

        var parameters = CreateModelParameters(chat, modelKey);
        var disableCache = chat.Properties.CheckProperty(ServiceConstants.Properties.DisableCacheProperty);
        var llmModel = disableCache
            ? await LLamaWeights.LoadFromFileAsync(parameters, cancellationToken)
            : await ModelLoader.GetOrLoadModelAsync(modelsPath, modelKey);

        var llavaWeights = model.MMProject != null
            ? await LLavaWeights.LoadFromFileAsync(model.MMProject, cancellationToken)
            : null;


        using var executor = new BatchedExecutor(llmModel, parameters);

        var (conversation, isComplete, hasFailed) = await InitializeConversation(
            chat, lastMsg, model, llmModel, llavaWeights, executor, cancellationToken);

        if (!isComplete)
        {
            (tokens, isComplete, hasFailed) = await ProcessTokens(
                chat, conversation, model, llmModel, executor, thinkingState, requestOptions, cancellationToken);
        }

        if (isComplete && !hasFailed)
        {
            if (requestOptions.SaveConv)
            {
                chat.ConversationState = conversation.Save();
            }

            if (isComplete)
            {
                conversation.Dispose();
                if (disableCache)
                {
                    llmModel.Dispose();
                }
            }
        }

        return tokens;
    }

    private ModelParams CreateModelParameters(Chat chat, string modelKey)
    {
        return new ModelParams(Path.Combine(modelsPath, modelKey))
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
    }

    private async Task<(Conversation Conversation, bool IsComplete, bool HasFailed)> InitializeConversation(Chat chat,
        Message lastMsg,
        Model model,
        LLamaWeights llmModel,
        LLavaWeights? llavaWeights,
        BatchedExecutor executor,
        CancellationToken cancellationToken)
    {
        var isNewConversation = chat.ConversationState == null;
        var conversation = isNewConversation
            ? executor.Create()
            : executor.Load(chat.ConversationState!);

        if (lastMsg.Image != null)
        {
            await ProcessImageMessage(conversation, lastMsg, llmModel, llavaWeights, executor, cancellationToken);
        }
        else
        {
            ProcessTextMessage(conversation, chat, lastMsg, model, llmModel, executor, isNewConversation);
        }

        return (conversation, false, false);
    }

    private static async Task ProcessImageMessage(Conversation conversation,
        Message lastMsg,
        LLamaWeights llmModel,
        LLavaWeights? llavaWeights,
        BatchedExecutor executor,
        CancellationToken cancellationToken)
    {
        var imageEmbeddings = llavaWeights?.CreateImageEmbeddings(lastMsg.Image!);
        conversation.Prompt(imageEmbeddings!);

        while (executor.BatchedTokenCount > 0)
            await executor.Infer(cancellationToken);

        var prompt = llmModel.Tokenize($"USER: {lastMsg.Content}\nASSISTANT:", true, false, Encoding.UTF8);
        conversation.Prompt(prompt);
    }

    private static void ProcessTextMessage(Conversation conversation,
        Chat chat,
        Message lastMsg,
        Model model,
        LLamaWeights llmModel,
        BatchedExecutor executor,
        bool isNewConversation)
    {
        var template = new LLamaTemplate(llmModel);
        var finalPrompt = ChatHelper.GetFinalPrompt(lastMsg, model, isNewConversation);

        if (isNewConversation)
        {
            foreach (var messageToProcess in chat.Messages
                         .Where(x => x.Properties.ContainsKey(Message.UnprocessedMessageProperty))
                         .SkipLast(1))
            {
                template.Add(messageToProcess.Role, messageToProcess.Content);
            }
        }

        template.Add(ServiceConstants.Roles.User, finalPrompt);
        template.AddAssistant = true;

        var templatedMessage = Encoding.UTF8.GetString(template.Apply());
        var tokens = isNewConversation
            ? executor.Context.Tokenize(templatedMessage, addBos: true, special: true)
            : executor.Context.Tokenize(templatedMessage);

        conversation.Prompt(tokens);
    }

    private async Task<(List<LLMTokenValue> Tokens, bool IsComplete, bool HasFailed)> ProcessTokens(
        Chat chat,
        Conversation conversation,
        Model model,
        LLamaWeights llmModel,
        BatchedExecutor executor,
        ThinkingState thinkingState,
        ChatRequestOptions requestOptions,
        CancellationToken cancellationToken)
    {
        var tokens = new List<LLMTokenValue>();
        var isComplete = false;
        var hasFailed = false;

        using var sampler = CreateSampler(chat.InterferenceParams);
        var decoder = new StreamingTokenDecoder(executor.Context);

        var inferenceParams = ChatHelper.CreateInferenceParams(chat, llmModel);
        var maxTokens = inferenceParams.MaxTokens == -1 ? int.MaxValue : inferenceParams.MaxTokens;

        for (var i = 0; i < maxTokens && !isComplete; i++)
        {
            var decodeResult = await executor.Infer(cancellationToken);

            if (decodeResult == DecodeResult.NoKvSlot)
            {
                isComplete = true;
                hasFailed = true;
                chat.ConversationState = null;
                break;
            }

            if (decodeResult == DecodeResult.DecodeFailed)
                throw new Exception("Unknown error occurred while inferring.");

            if (!conversation.RequiresSampling)
                continue;

            var token = conversation.Sample(sampler);
            var vocab = executor.Context.NativeHandle.ModelHandle.Vocab;

            if (token.IsEndOfGeneration(vocab))
            {
                isComplete = true;
            }
            else
            {
                decoder.Add(token);
                var tokenTxt = decoder.Read();

                conversation.Prompt(token);
                var tokenValue = model.ReasonFunction != null
                    ? model.ReasonFunction(tokenTxt, thinkingState)
                    : new LLMTokenValue()
                    {
                        Text = tokenTxt,
                        Type = TokenType.Message
                    };

                tokens.Add(tokenValue);

                if (requestOptions.InteractiveUpdates)
                {
                    await SendNotification(chat.Id, tokenValue, false);
                }

                requestOptions.TokenCallback?.Invoke(tokenValue);
            }
        }
        

        return (tokens, isComplete, hasFailed);
    }

    private BaseSamplingPipeline CreateSampler(InferenceParams interferenceParams)
    {
        if (interferenceParams.Temperature == 0)
        {
            return new GreedySamplingPipeline()
            {
                Grammar = interferenceParams.Grammar != null ? new Grammar(interferenceParams.Grammar, "root") : null
            };
        }

        return new DefaultSamplingPipeline()
        {
            Temperature = interferenceParams.Temperature,
            TopP = interferenceParams.TopP,
            TopK = interferenceParams.TopK,
            Grammar = interferenceParams.Grammar != null ? new Grammar(interferenceParams.Grammar, "root") : null
        };
    }


    private string GetModelsPath()
    {
        var path = options.ModelsPath ?? Environment.GetEnvironmentVariable(DEFAULT_MODEL_ENV_PATH);
        if (string.IsNullOrEmpty(path))
        {
            throw new InvalidOperationException("Models path not found in configuration or environment variables");
        }

        return path;
    }

    private async Task<ChatResult> CreateChatResult(Chat chat, List<LLMTokenValue> tokens,
        ChatRequestOptions requestOptions)
    {
        var responseText = string.Concat(tokens.Select(x => x.Text));

        if (requestOptions.InteractiveUpdates)
        {
            await SendNotification(chat.Id, new LLMTokenValue
            {
                Type = TokenType.FullAnswer,
                Text = responseText
            }, true);
        }

        return new ChatResult
        {
            Done = true,
            CreatedAt = DateTime.Now,
            Model = chat.Model,
            Message = new Message
            {
                Content = responseText,
                Tokens = tokens,
                Role = AuthorRole.Assistant.ToString(),
                Type = MessageType.LocalLLM,
            }.MarkProcessed()
        };
    }

    private async Task SendNotification(string chatId, LLMTokenValue token, bool isComplete)
    {
        await notificationService.DispatchNotification(
            NotificationMessageBuilder.CreateChatCompletion(chatId, token, isComplete),
            ServiceConstants.Notifications.ReceiveMessageUpdate);
    }
}