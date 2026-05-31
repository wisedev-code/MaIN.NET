using FuzzySharp;
using MaIN.Core.Hub;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace MaIN.Core.E2ETests;

/// <summary>
/// Demonstrates IMaINHub wired through standard ASP.NET Core minimal API DI.
/// The endpoint injects IMaINHub, reads a small JSON file, and asks a local model
/// to extract a single field (car color).
///
/// Requires: Qwen2.5-0.5b downloaded locally.
/// This test is excluded from CI — run it manually alongside the other E2E tests.
/// </summary>
[Collection("E2ETests")]
public class MinimalApiCarColorTests : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly HttpClient _client;

    public MinimalApiCarColorTests()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            // Use random available port so multiple test runs don't collide.
            Args = ["--urls", "http://127.0.0.1:0"]
        });

        builder.Services.AddMaIN(builder.Configuration);

        _app = builder.Build();
        _app.Services.UseMaIN();

        _app.MapGet("/car/color", async (IMaINHub hub) =>
        {
            var carJson = await File.ReadAllTextAsync("./Files/Car.json");

            var result = await hub.Chat()
                .WithModel(Models.Local.Qwen2_5_0_5b)
                .WithMessage($"From this car JSON return ONLY the color value, one word, lowercase:\n{carJson}")
                .WithSystemPrompt("You are a data extraction assistant. Reply with a single word only.")
                .WithMemoryParams(new MemoryParams { AnswerTokens = 10 })
                .CompleteAsync();

            return Results.Ok(result.Message.Content.Trim());
        });

        _app.Start();

        var address = _app.Urls.First();
        _client = new HttpClient { BaseAddress = new Uri(address) };
    }

    [Fact]
    public async Task CarColorEndpoint_ReturnsCorrectColor_ViaInjectedHub()
    {
        var response = await _client.GetStringAsync("/car/color");

        Assert.NotNull(response);
        Assert.NotEmpty(response);

        var ratio = Fuzz.PartialRatio("red", response.ToLowerInvariant());
        Assert.True(ratio > 50,
            $"""
            Fuzzy match failed!
            Expected > 50, but got {ratio}.
            Expected color: 'red'
            Actual response: '{response}'
            """);
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }
}
