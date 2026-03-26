using System.Text;
using MaIN.Domain.Configuration;
using MaIN.Domain.Configuration.BackendInferenceParams;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using MaIN.Domain.Models.Concrete;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService.Auth;
using MaIN.Services.Services.LLMService.Memory;
using MaIN.Services.Services.Models;
using MaIN.Services.Utils;
using Microsoft.Extensions.Logging;

namespace MaIN.Services.Services.LLMService;

public sealed class VertexService(
    MaINSettings settings,
    INotificationService notificationService,
    IHttpClientFactory httpClientFactory,
    IMemoryFactory memoryFactory,
    IMemoryService memoryService,
    ILogger<VertexService>? logger = null)
    : OpenAiCompatibleService(notificationService, httpClientFactory, memoryFactory, memoryService, logger), ILLMService
{
    private readonly MaINSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    private GoogleServiceAccountTokenProvider? _tokenProvider;
    private string _location = "us-central1";

    protected override string HttpClientName => ServiceConstants.HttpClients.VertexClient;

    protected override string ChatCompletionsUrl
    {
        get
        {
            var auth = _settings.GoogleServiceAccountAuth ?? throw new InvalidOperationException("MaINSettings.GoogleServiceAccountConfig is not configured.");
            return $"https://{_location}-aiplatform.googleapis.com/v1beta1/projects/{auth.ProjectId}/locations/{_location}/endpoints/openapi/chat/completions";
        }
    }

    protected override string ModelsUrl
    {
        get
        {
            var auth = _settings.GoogleServiceAccountAuth ?? throw new InvalidOperationException("MaINSettings.GoogleServiceAccountConfig is not configured.");
            return $"https://{_location}-aiplatform.googleapis.com/v1beta1/projects/{auth.ProjectId}/locations/{_location}/endpoints/openapi/models";
        }
    }

    protected override Type ExpectedParamsType => typeof(VertexInferenceParams);

    protected override string GetApiKey()
    {
        var auth = _settings.GoogleServiceAccountAuth ?? throw new InvalidOperationException("MaINSettings.VertexAuth is not configured.");

        _tokenProvider ??= new GoogleServiceAccountTokenProvider(auth);

        logger?.LogInformation("Vertex: Requesting access token for {ClientEmail}...", auth.ClientEmail);
        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        // Use Task.Run to avoid deadlocking on Blazor Server's SynchronizationContext
        var token = Task.Run(() => _tokenProvider.GetAccessTokenAsync(httpClient)).GetAwaiter().GetResult();
        logger?.LogInformation("Vertex: Access token obtained (length={Length})", token?.Length ?? 0);
        return token;
    }

    protected override string GetApiName() => LLMApiRegistry.Vertex.ApiName;

    protected override void ValidateApiKey()
    {
        var auth = _settings.GoogleServiceAccountAuth;
        if (auth == null)
            throw new InvalidOperationException("MaINSettings.GoogleServiceAccountConfig is not configured.");
        if (string.IsNullOrEmpty(auth.ProjectId))
            throw new InvalidOperationException("GoogleServiceAccountConfig.ProjectId is required.");
        if (string.IsNullOrEmpty(auth.ClientEmail))
            throw new InvalidOperationException("GoogleServiceAccountConfig.ClientEmail is required.");
        if (string.IsNullOrEmpty(auth.PrivateKey))
            throw new InvalidOperationException("GoogleServiceAccountConfig.PrivateKey is required.");
    }

    protected override void ApplyBackendParams(Dictionary<string, object> requestBody, Chat chat)
    {
        if (chat.BackendParams is not VertexInferenceParams p) return;
        if (p.Temperature.HasValue) requestBody["temperature"] = p.Temperature.Value;
        if (p.MaxTokens.HasValue) requestBody["max_tokens"] = p.MaxTokens.Value;
        if (p.TopP.HasValue) requestBody["top_p"] = p.TopP.Value;
        if (p.StopSequences is { Length: > 0 }) requestBody["stop"] = p.StopSequences;
    }

    public new async Task<ChatResult?> Send(
        Chat chat,
        ChatRequestOptions options,
        CancellationToken cancellationToken = default)
    {
        ExtractLocation(chat);
        logger?.LogInformation("Vertex: Send called, model={Model}, location={Location}, url={Url}",
            chat.ModelId, _location, ChatCompletionsUrl);
        return await base.Send(chat, options, cancellationToken);
    }

    /// <summary>
    /// Bypasses KernelMemory and sends files directly to Gemini via multimodal API.
    /// PDFs and images are sent inline (Gemini handles OCR natively),
    /// other formats are pre-processed to text via DocumentProcessor.
    /// </summary>
    public override async Task<ChatResult?> AskMemory(
        Chat chat,
        ChatMemoryOptions memoryOptions,
        ChatRequestOptions requestOptions,
        CancellationToken cancellationToken = default)
    {
        ExtractLocation(chat);

        if (!chat.Messages.Any())
            return null;

        var lastMessage = chat.Messages.Last();
        var originalContent = lastMessage.Content;
        var originalFiles = lastMessage.Files;
        var originalImages = lastMessage.Images;

        try
        {
            var inlineBytes = new List<byte[]>();
            var textContext = new StringBuilder();

            CollectTextData(memoryOptions, textContext);
            await CollectFilesData(memoryOptions, inlineBytes, textContext, cancellationToken);
            await CollectStreamData(memoryOptions, inlineBytes, textContext, cancellationToken);
            CollectMemoryItems(memoryOptions, textContext);

            var queryBuilder = new StringBuilder();
            if (textContext.Length > 0)
            {
                queryBuilder.AppendLine("Use the following document content to answer the question:\n");
                queryBuilder.Append(textContext);
                queryBuilder.AppendLine();
            }
            queryBuilder.Append(originalContent);

            if (chat.MemoryParams.Grammar != null)
            {
                var jsonGrammar = new GrammarToJsonConverter().ConvertToJson(chat.MemoryParams.Grammar);
                queryBuilder.Append(
                    $" | For your next response only, please respond using exactly the following JSON format: \n{jsonGrammar}\n. Do not include any explanations, code blocks, or additional content. After this single JSON response, resume your normal conversational style.");
            }

            lastMessage.Content = queryBuilder.ToString();
            lastMessage.Files = null;

            // Merge existing images with inline file bytes (PDFs sent as native multimodal content)
            var allInline = new List<byte[]>(originalImages ?? []);
            allInline.AddRange(inlineBytes);
            lastMessage.Images = allInline.Count > 0 ? allInline : null;

            return await Send(chat, requestOptions, cancellationToken);
        }
        finally
        {
            lastMessage.Content = originalContent;
            lastMessage.Files = originalFiles;
            lastMessage.Images = originalImages;
        }
    }

    #region Multimodal File Processing

    private static readonly HashSet<string> GeminiNativeExtensions =
        [".pdf", ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".tiff", ".tif", ".heic", ".heif", ".avif"];

    private static bool IsGeminiNativeFile(string fileName)
        => GeminiNativeExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant());

    private static void CollectTextData(ChatMemoryOptions options, StringBuilder textContext)
    {
        foreach (var (name, content) in options.TextData)
        {
            textContext.AppendLine($"[Document: {name}]");
            textContext.AppendLine(content);
            textContext.AppendLine();
        }
    }

    private static async Task CollectFilesData(
        ChatMemoryOptions options, List<byte[]> inlineBytes, StringBuilder textContext,
        CancellationToken cancellationToken)
    {
        foreach (var (name, path) in options.FilesData)
        {
            if (IsGeminiNativeFile(name))
            {
                inlineBytes.Add(await File.ReadAllBytesAsync(path, cancellationToken));
            }
            else
            {
                textContext.AppendLine($"[Document: {name}]");
                textContext.AppendLine(DocumentProcessor.ProcessDocument(path));
                textContext.AppendLine();
            }
        }
    }

    private static async Task CollectStreamData(
        ChatMemoryOptions options, List<byte[]> inlineBytes, StringBuilder textContext,
        CancellationToken cancellationToken)
    {
        foreach (var (name, stream) in options.StreamData)
        {
            using var ms = new MemoryStream();
            if (stream.CanSeek) stream.Position = 0;
            await stream.CopyToAsync(ms, cancellationToken);
            var bytes = ms.ToArray();

            if (IsGeminiNativeFile(name))
            {
                inlineBytes.Add(bytes);
            }
            else
            {
                var tempPath = Path.Combine(Path.GetTempPath(), $"vertex_tmp_{Guid.NewGuid()}{Path.GetExtension(name)}");
                try
                {
                    await File.WriteAllBytesAsync(tempPath, bytes, cancellationToken);
                    textContext.AppendLine($"[Document: {name}]");
                    textContext.AppendLine(DocumentProcessor.ProcessDocument(tempPath));
                    textContext.AppendLine();
                }
                finally
                {
                    if (File.Exists(tempPath)) File.Delete(tempPath);
                }
            }
        }
    }

    private static void CollectMemoryItems(ChatMemoryOptions options, StringBuilder textContext)
    {
        if (options.Memory is not { Count: > 0 }) return;
        foreach (var item in options.Memory)
        {
            textContext.AppendLine(item);
            textContext.AppendLine();
        }
    }

    #endregion

    private void ExtractLocation(Chat chat)
    {
        if (chat.BackendParams is VertexInferenceParams vp)
            _location = vp.Location;
    }
}
