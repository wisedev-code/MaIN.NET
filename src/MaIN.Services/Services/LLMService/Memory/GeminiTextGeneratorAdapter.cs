using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.TextGeneration;

namespace MaIN.Services.Services.LLMService.Memory;

internal class GeminiTextGeneratorAdapter : ITextGenerationService
{
    private readonly IChatCompletionService _geminiChatService;
    public IReadOnlyDictionary<string, object?> Attributes { get; }

    public GeminiTextGeneratorAdapter(IChatCompletionService geminiChatService)
    {
        _geminiChatService = geminiChatService;
    }

    public async Task<IReadOnlyList<TextContent>> GetTextContentsAsync(string prompt, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = new CancellationToken())
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt);

        var chatMessageContents = await _geminiChatService.GetChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken);
        var textContents = new List<TextContent>();

        foreach (var chatMessageContent in chatMessageContents)
        {
            var textContent = new TextContent(chatMessageContent.Content);
            textContents.Add(textContent);
        }

        return textContents;
    }

    public async IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(string prompt, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = new CancellationToken())
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt);

        var currentExecutionSettings = executionSettings ?? new PromptExecutionSettings();
        
        await foreach (var streamingChatMessageContent in _geminiChatService.GetStreamingChatMessageContentsAsync(
                           chatHistory,
                           currentExecutionSettings,
                           kernel,
                           cancellationToken))
        {
            yield return new StreamingTextContent(
                text: streamingChatMessageContent.Content,
                choiceIndex: streamingChatMessageContent.ChoiceIndex,
                modelId: streamingChatMessageContent.ModelId,
                metadata: streamingChatMessageContent.Metadata);
        }
    }
}