using LLama.Common;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService.Utils;
using MaIN.Services.Services.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0070

namespace MaIN.Services.Services;

public class McpService(MaINSettings settings, IServiceProvider serviceProvider) : IMcpService
{
    public async Task<McpResult> Prompt(Mcp config, string prompt)
    {
        await using var mcpClient = await McpClientFactory.CreateAsync(
            new StdioClientTransport(
                new StdioClientTransportOptions
                {
                    Command = config.Command,
                    Arguments = config.Arguments,
                    EnvironmentVariables = config.EnvironmentVariables!
                })
        );

        var builder = Kernel.CreateBuilder();
        var promptSettings = InitializeChatCompletions(builder, config.Backend ?? settings.BackendType, config.Model);
        var kernel = builder.Build();
        var tools = await mcpClient.ListToolsAsync();
        kernel.Plugins.AddFromFunctions("Tools", tools.Select(x => x.AsKernelFunction()));

        var res = await kernel.InvokePromptAsync(prompt,new KernelArguments(promptSettings));

        return new McpResult
        {
            CreatedAt = DateTime.Now,
            Message = new Message
            {
                Content = res.ToString(),
                Role = nameof(AuthorRole.Assistant),
                Type = MessageType.CloudLLM
            },
            Model = config.Model
        };
    }

    private PromptExecutionSettings InitializeChatCompletions(IKernelBuilder kernelBuilder, BackendType backendType, string model)
    {
        switch (backendType)
        {
            case BackendType.OpenAi:
                kernelBuilder.Services.AddOpenAIChatCompletion(model, GetOpenAiKey() ?? throw new ArgumentNullException(nameof(GetOpenAiKey)));
                return new OpenAIPromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true })
                };

            case BackendType.Gemini:
                kernelBuilder.Services.AddGoogleAIGeminiChatCompletion(model, GetGeminiKey() ?? throw new ArgumentNullException(nameof(GetGeminiKey)));
                return new GeminiPromptExecutionSettings
                {
                    ModelId = model,
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true })
                };

            case BackendType.DeepSeek:
                throw new NotSupportedException("DeepSeek models does not support MCP integration.");

            case BackendType.GroqCloud:
                kernelBuilder.Services.AddOpenAIChatCompletion(
                    modelId: model,
                    apiKey: GetGroqCloudKey() ?? throw new ArgumentNullException(nameof(GetGroqCloudKey)),
                    endpoint: new Uri("https://api.groq.com/openai/v1"));

                return new OpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true })
                };

            case BackendType.Anthropic:
                kernelBuilder.AddAnthropicChatCompletion(serviceProvider, model, GetAnthropicKey() ?? throw new ArgumentNullException(nameof(GetAnthropicKey)));
                return new PromptExecutionSettings
                {
                    ExtensionData = new Dictionary<string, object>{ ["max_tokens"] = 4096 }
                };

            case BackendType.Self:
                throw new NotSupportedException("Self backend (local models) does not support MCP integration.");

            default:
                throw new ArgumentOutOfRangeException(nameof(backendType));
        }
    }

    string? GetOpenAiKey()
        => settings.OpenAiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    string? GetGeminiKey()
        => settings.GeminiKey ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    string? GetGroqCloudKey()
        => settings.GroqCloudKey ?? Environment.GetEnvironmentVariable("GROQ_API_KEY");
    string? GetAnthropicKey()
        => settings.AnthropicKey ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
}