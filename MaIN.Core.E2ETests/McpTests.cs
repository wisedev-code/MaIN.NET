using MaIN.Core.Hub;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using MaIN.Domain.Models.Concrete;

namespace MaIN.Core.E2ETests;

[Collection("E2ETests")]
public class McpTests : IntegrationTestBase
{
    private const string McpPrompt =
        "Generate a fun fact (2-3 sentences, genuinely surprising) and write it to {0} using the write_file tool. " +
        "After writing, confirm what you saved and share the fun fact.";

    [SkippableFact]
    public async Task OpenAi_Mcp_Should_WriteFileAndReturnContent()
    {
        SkipIfMissingKey(LLMApiRegistry.GetEntry(BackendType.OpenAi)?.ApiKeyEnvName!);

        var tempDir = CreateTempDir();
        try
        {
            var filePath = Path.Combine(tempDir, "funfact.txt").Replace('\\', '/');
            var result = await AIHub.Mcp()
                .WithBackend(BackendType.OpenAi)
                .WithConfig(new Mcp
                {
                    Name = "filesystem",
                    Command = "npx",
                    Arguments = ["-y", "@modelcontextprotocol/server-filesystem", tempDir],
                    Model = Models.OpenAi.Gpt4oMini
                })
                .PromptAsync(string.Format(McpPrompt, filePath));

            Assert.NotNull(result);
            Assert.NotEmpty(result.Message.Content);
            Assert.True(File.Exists(filePath), $"Expected file at {filePath}");
            Assert.NotEmpty(await File.ReadAllTextAsync(filePath));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [SkippableFact]
    public async Task Gemini_Mcp_Should_WriteFileAndReturnContent()
    {
        SkipIfMissingKey(LLMApiRegistry.GetEntry(BackendType.Gemini)?.ApiKeyEnvName!);

        var tempDir = CreateTempDir();
        try
        {
            var filePath = Path.Combine(tempDir, "funfact.txt").Replace('\\', '/');
            var result = await AIHub.Mcp()
                .WithBackend(BackendType.Gemini)
                .WithConfig(new Mcp
                {
                    Name = "filesystem",
                    Command = "npx",
                    Arguments = ["-y", "@modelcontextprotocol/server-filesystem", tempDir],
                    Model = Models.Gemini.Gemini2_0Flash
                })
                .PromptAsync(string.Format(McpPrompt, filePath));

            Assert.NotNull(result);
            Assert.NotEmpty(result.Message.Content);
            Assert.True(File.Exists(filePath), $"Expected file at {filePath}");
            Assert.NotEmpty(await File.ReadAllTextAsync(filePath));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [SkippableFact]
    public async Task Anthropic_Mcp_Should_WriteFileAndReturnContent()
    {
        SkipIfMissingKey(LLMApiRegistry.GetEntry(BackendType.Anthropic)?.ApiKeyEnvName!);

        var tempDir = CreateTempDir();
        try
        {
            var filePath = Path.Combine(tempDir, "funfact.txt").Replace('\\', '/');
            var result = await AIHub.Mcp()
                .WithBackend(BackendType.Anthropic)
                .WithConfig(new Mcp
                {
                    Name = "filesystem",
                    Command = "npx",
                    Arguments = ["-y", "@modelcontextprotocol/server-filesystem", tempDir],
                    Model = Models.Anthropic.ClaudeSonnet4
                })
                .PromptAsync(string.Format(McpPrompt, filePath));

            Assert.NotNull(result);
            Assert.NotEmpty(result.Message.Content);
            Assert.True(File.Exists(filePath), $"Expected file at {filePath}");
            Assert.NotEmpty(await File.ReadAllTextAsync(filePath));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"mcp-e2e-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void SkipIfMissingKey(string envName)
    {
        Skip.If(string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envName)),
            $"{envName} environment variable not set");
    }
}
