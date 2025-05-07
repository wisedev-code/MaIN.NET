using System.Collections.Concurrent;
using System.Text;
using LLama;
using LLama.Batched;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;
using LLama.Transformers;
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

        var model = KnownModels.GetModel(chat.Model);
        var modelKey = model.FileName;
        var thinkingState = new ThinkingState();
        var lastMsg = chat.Messages.Last();
        var tokens = new List<LLMTokenValue>();

        if (ChatHelper.HasFiles(lastMsg))
        {
            var memoryOptions = ChatHelper.ExtractMemoryOptions(lastMsg);
            return await AskMemory(chat, memoryOptions, cancellationToken);
        }

        var parameters = new ModelParams(Path.Combine(modelsPath, modelKey))
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

        var llmModel = await ModelLoader.GetOrLoadModelAsync(modelsPath, modelKey);    
        using var executor = new BatchedExecutor(llmModel, parameters);

        Conversation conversation;
        bool isNewConversation = chat.ConversationState == null;

        if (isNewConversation)
        {
            var systemMsg = chat.Messages.FirstOrDefault(x => x.Role == nameof(AuthorRole.System));
            var template = new LLamaTemplate(llmModel);
            var finalPrompt = ChatHelper.GetFinalPrompt(lastMsg, model, true);
            if (systemMsg != null)
            {
                template.Add(systemMsg.Role, systemMsg.Content);
            }
            
            template.Add(ServiceConstants.Roles.User, finalPrompt);
            template.AddAssistant = true;

            var templatedMessage = Encoding.UTF8.GetString(template.Apply());
            conversation = executor.Create();
            conversation.Prompt(executor.Context.Tokenize(templatedMessage, addBos: true, special: true));
        }
        else
        {
            conversation = executor.Load(chat.ConversationState!);
            var template = new LLamaTemplate(llmModel);
            var finalPrompt = ChatHelper.GetFinalPrompt(lastMsg, model, false);
            template.Add(ServiceConstants.Roles.User, finalPrompt);
            template.AddAssistant = true;
            var templatedMessage = Encoding.UTF8.GetString(template.Apply());

            conversation.Prompt(executor.Context.Tokenize(templatedMessage));
        }

        using var sampler = CreateSampler(chat.InterferenceParams);
        var decoder = new StreamingTokenDecoder(executor.Context);
        var isComplete = false;
        var hasFailed = false;

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

            if (decodeResult == DecodeResult.Error)
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

        if (isComplete && !hasFailed)
        {
            chat.ConversationState = conversation.Save();
            if (isComplete)
            {
                conversation.Dispose();
            }
        }

        return await CreateChatResult(chat, tokens, requestOptions);
    }

    private BaseSamplingPipeline CreateSampler(InferenceParams interferenceParams)
    {
        if (interferenceParams.Temperature == 0)
        {
            return new GreedySamplingPipeline();
        }

        return new DefaultSamplingPipeline()
        {
            Temperature = interferenceParams.Temperature,
            TopP = interferenceParams.TopP,
            TopK = interferenceParams.TopK
        };
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

        session.Executor.Context.NativeHandle.KvCacheClear();
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
        using var llmModel = await LLamaWeights.LoadFromFileAsync(parameters, cancellationToken);
        var kernelMemory = memoryFactory.CreateMemoryWithModel(
            modelsPath,
            llmModel,
            chat.MemoryParams);

        await memoryService.ImportDataToMemory(kernelMemory, memoryOptions, cancellationToken);
        var userMessage = chat.Messages.Last();
        var result = await kernelMemory.AskAsync(
            userMessage.Content,
            cancellationToken: cancellationToken);
        await kernelMemory.DeleteIndexAsync(cancellationToken: cancellationToken);

        return new ChatResult
        {
            Done = true,
            CreatedAt = DateTime.Now,
            Model = chat.Model,
            Message = new Message
            {
                Content = memoryService.CleanResponseText(result.Result),
                Role = nameof(AuthorRole.Assistant)
            }
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

    private ChatSession GetOrCreateSession(string chatId, Func<ChatSession> createSession)
    {
        return _sessionCache.GetOrAdd(chatId, _ => createSession());
    }

    private ChatSession CreateNewSession(LLamaWeights model, Chat chat)
    {
        var parameters = CreateModelParameters(model, chat);
        var context = model.CreateContext(parameters);
        var history = new ChatHistory();
        var executor = new InteractiveExecutor(context);
        return new ChatSession(executor, history);
    }

    private ModelParams CreateModelParameters(LLamaWeights model, Chat chat)
    {
        return new ModelParams(Path.Combine(modelsPath, chat.Model))
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

    private void ConfigureSession(ChatSession session, LLamaWeights model)
    {
        session.WithHistoryTransform(new PromptTemplateTransformer(model, withAssistant: true));
        session.WithOutputTransform(new LLamaTransforms.KeywordTextOutputStreamTransform(
            [model.Vocab.EOT.ToString() ?? "User:", "�", "Assistant:"],
            redundancyLength: 5));
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

    private async Task<List<LLMTokenValue>> ProcessChatRequest(
        Chat chat,
        ChatSession session,
        Model model,
        LLamaWeights llmModel,
        bool startSession,
        ThinkingState thinkingState,
        ChatRequestOptions requestOptions,
        CancellationToken cancellationToken)
    {
        var tokens = new List<LLMTokenValue>();
        var lastMessage = chat.Messages.Last();

        if (ChatHelper.HasFiles(lastMessage))
        {
            var memoryOptions = ChatHelper.ExtractMemoryOptions(lastMessage);
            var result = await AskMemory(chat, memoryOptions, cancellationToken);

            if (result?.Message.Content != null)
            {
                tokens.Add(new LLMTokenValue
                {
                    Type = TokenType.FullAnswer,
                    Text = result.Message.Content
                });
            }

            return tokens;
        }

        var finalPrompt = ChatHelper.GetFinalPrompt(lastMessage, model, startSession);
        var inferenceParams = ChatHelper.CreateInferenceParams(chat, llmModel);

        await foreach (var text in session.ChatAsync(
                           new ChatHistory.Message(AuthorRole.User, finalPrompt),
                           inferenceParams,
                           cancellationToken))
        {
            var token = model.ReasonFunction != null
                ? model.ReasonFunction(text, thinkingState)
                : new LLMTokenValue
                {
                    Type = TokenType.Message,
                    Text = text
                };

            if (requestOptions.InteractiveUpdates)
            {
                await SendNotification(chat.Id, token, false);
            }

            requestOptions.TokenCallback?.Invoke(token);
            tokens.Add(token);
        }

        return tokens;
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
                Role = AuthorRole.Assistant.ToString()
            }
        };
    }

    private async Task SendNotification(string chatId, LLMTokenValue token, bool isComplete)
    {
        await notificationService.DispatchNotification(
            NotificationMessageBuilder.CreateChatCompletion(chatId, token, isComplete),
            ServiceConstants.Notifications.ReceiveMessageUpdate);
    }
}

public class ConversationData
{
    public required string Prompt { get; init; }
    public required Conversation Conversation { get; init; }
    public required BaseSamplingPipeline Sampler { get; init; }
    public required StreamingTokenDecoder Decoder { get; init; }

    // public string AnswerMarkdown =>
    //     IsComplete
    //         ? $"[{(IsFailed ? "red" : "green")}]{_inProgressAnswer.Message.EscapeMarkup()}{_inProgressAnswer.LatestToken.EscapeMarkup()}[/]"
    //         : $"[grey]{_inProgressAnswer.Message.EscapeMarkup()}[/][white]{_inProgressAnswer.LatestToken.EscapeMarkup()}[/]";

    public bool IsComplete { get; private set; }
    public bool IsFailed { get; private set; }

    // we are only keeping track of the answer in two parts to render them differently.
    private (string Message, string LatestToken) _inProgressAnswer = (string.Empty, string.Empty);

    public void AppendAnswer(string newText) =>
        _inProgressAnswer = (_inProgressAnswer.Message + _inProgressAnswer.LatestToken, newText);

    public void MarkComplete(bool failed = false)
    {
        IsComplete = true;
        IsFailed = failed;
        if (Conversation.IsDisposed == false)
        {
            // clean up the conversation and sampler to release more memory for inference. 
            // real life usage would protect against these two being referenced after being disposed.
            Conversation.Dispose();
            Sampler.Dispose();
        }
    }
}