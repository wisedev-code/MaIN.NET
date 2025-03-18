using System.Text.Json;
using MaIN.Core.Hub;
using MaIN.Domain.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace MaIN.Core.IntegrationTests;

public class ChatTests : IntegrationTestBase
{
    public ChatTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }
    
    [Fact]
    public async Task Should_AnswerQuestion_BasicChat()
    {
        var context = AIHub.Chat().WithModel("gemma2:2b");
        
        var result = await context
            .WithMessage("Where the hedgehog goes at night?")
            .CompleteAsync(interactive: true);

        Assert.True(result.Done);
        Assert.NotNull(result.Message);
        Assert.NotEmpty(result.Message.Content);
    }

    [Fact]
    public async Task Should_AnswerDifferences_BetweenDocuments_ChatWithFiles()
    {
        List<string> files = ["./Files/Nicolaus_Copernicus.pdf", "./Files/Galileo_Galilei.pdf"];
        
        var result = await AIHub.Chat()
            .WithModel("gemma2:2b")
            .WithMessage("You have 2 documents in memory. Whats the difference of work between Galileo and Copernicus?. Give answer based on the documents.")
            .WithFiles(files)
            .CompleteAsync();
        
        Assert.True(result.Done);
        Assert.NotNull(result.Message);
        Assert.NotEmpty(result.Message.Content);
    }

    [Fact]
    public async Task Should_AnswerQuestion_FromExistingChat()
    {
        var result = AIHub.Chat()
            .WithModel("qwen2.5:0.5b");
        
        await result.WithMessage("What do you think about math theories?")
            .CompleteAsync();
        
        await result.WithMessage("And about physics?")
            .CompleteAsync();

        var chatNewContext = await AIHub.Chat().FromExisting(result.GetChatId());
        var messages = chatNewContext.GetChatHistory();

        Assert.Equal(4, messages.Count);
    }

    [Fact]
    public async Task Should_AnswerGameFromImage_ChatWithVision()
    {
        List<string> images = ["./Files/gamex.jpg"];
        
        var result = await AIHub.Chat()
            .WithModel("llama3.2:3b")
            .WithMessage("What is the title of game?")
            .WithFiles(images)
            .CompleteAsync();
        
        Assert.True(result.Done);
        Assert.NotNull(result.Message);
        Assert.NotEmpty(result.Message.Content);
        Assert.Contains("call of duty", result.Message.Content.ToLower());
    }

    //[Fact]
    public async Task Should_GenerateImage_BasedOnPrompt()
    {
        Assert.True(PingHost("127.0.0.1", 5003, 5), "Please make sure ImageGen service is running on port 5003");
        
        const string extension = "png";
        
        var result = await AIHub.Chat()
            .EnableVisual()
            .WithMessage("Generate cat in Rome. Sightseeing, colloseum, ancient builidngs, Italy.")
            .CompleteAsync();

        if (string.IsNullOrWhiteSpace(extension) || extension.Contains("."))
            throw new ArgumentException("Invalid file extension");

        Assert.True(result.Done);
        Assert.NotNull(result.Message.Images);
    }
}