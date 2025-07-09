using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.TextGeneration;

namespace MaIN.Services.Services.LLMService.Memory;

internal class GeminiTextGeneratorAdapter(IChatCompletionService geminiChatService) : ITextGenerationService
{
    public IReadOnlyDictionary<string, object?> Attributes { get; } = null!;

    public async Task<IReadOnlyList<TextContent>> GetTextContentsAsync(string prompt,
        PromptExecutionSettings? executionSettings = null, Kernel? kernel = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt);

        var chatMessageContents =
            await geminiChatService.GetChatMessageContentsAsync(chatHistory, executionSettings, kernel,
                cancellationToken);
        var textContents = new List<TextContent>();

        foreach (var chatMessageContent in chatMessageContents)
        {
            var textContent = new TextContent(chatMessageContent.Content);
            textContents.Add(textContent);
        }

        return textContents;
    }

    public async IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(
        string prompt,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = new CancellationToken())
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt);

        var currentExecutionSettings = executionSettings ?? new PromptExecutionSettings();

        await foreach (var streamingChatMessageContent in geminiChatService.GetStreamingChatMessageContentsAsync(
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