using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace MaIN.Services.Services.LLMService.Utils;

internal static class KernelBuilderExtensions
{
    public static IKernelBuilder AddAnthropicChatCompletion(this IKernelBuilder builder, IServiceProvider serviceProvider, string modelId, string apiKey)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        var chatService = new AnthropicChatCompletionService(new Logger<AnthropicChatCompletionService>(loggerFactory), httpClientFactory, modelId, apiKey);
        builder.Services.AddSingleton<IChatCompletionService>(chatService);

        return builder;
    }
}