using LLama.Common;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Services.Services.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0070

namespace MaIN.Services.Services;

public interface IMcpService
{
    Task<McpResult> Prompt(Mcp config, string prompt);
}

public class McpService(MaINSettings settings) : IMcpService
{
    public async Task<McpResult> Prompt(Mcp config, string prompt)
    {
        await using var mcpClient = await McpClientFactory.CreateAsync(
            new StdioClientTransport(
                new StdioClientTransportOptions()
                {
                    Command = config.Command,
                    Arguments = config.Arguments,
                    EnvironmentVariables = config.EnvironmentVariables!
                })
        );

        var builder = Kernel.CreateBuilder();
        InitializeChatCompletions(builder, config.Backend ?? settings.BackendType, config.Model);
        var kernel = builder.Build();
        var tools = await mcpClient.ListToolsAsync();
        kernel.Plugins.AddFromFunctions(config.Name, tools.Select(x => x.AsKernelFunction()));
        var settings2 = new OpenAIPromptExecutionSettings()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };
        var res = await kernel.InvokePromptAsync(prompt,new KernelArguments(settings2));

        return new McpResult()
        {
            CreatedAt = DateTime.Now,
            Message = new Message()
            {
                Content = res.ToString(),
                Role = nameof(AuthorRole.Assistant),
            },
            Model = config.Model
        };
    }

    private void InitializeChatCompletions(IKernelBuilder kernelBuilder, BackendType backendType, string model)
    {
        if (backendType == BackendType.Self)
        {
            throw new NotSupportedException("Self backend (local models) does not support MCP integration.");
        }

        switch (backendType)
        {
            case BackendType.OpenAi:
                kernelBuilder.Services.AddOpenAIChatCompletion(model, GetOpenAiKey() ?? throw new ArgumentNullException(nameof(GetOpenAiKey)));
                break;
            case BackendType.Gemini:
                kernelBuilder.Services.AddGoogleAIGeminiChatCompletion(model, GetGeminiKey() ?? throw new ArgumentNullException(nameof(GetGeminiKey)));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(backendType));
        }
    }

    string? GetOpenAiKey()
        => settings.OpenAiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    string? GetGeminiKey()
        => settings.GeminiKey ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
}