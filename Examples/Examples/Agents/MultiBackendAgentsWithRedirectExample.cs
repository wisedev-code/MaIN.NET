using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;
using MaIN.Domain.Configuration;

namespace Examples.Agents;

public class MultiBackendAgentWithRedirectExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Basic agent&friends example is running!");

        var systemPrompt =
            """
            You are a refined poet with a mastery of elegant English. Your verses should be lyrical,
            evocative, and rich in imagery. Maintain a graceful rhythm, sophisticated vocabulary,
            and a touch of timeless beauty in every poem you compose.
            """;
        
        var systemPromptSecond =
            """
            You are a modern rap lyricist with a sharp, streetwise flow. Take the given poem and transform
            it into raw, rhythmic bars filled with swagger, energy, and contemporary slang. 
            Maintain the core meaning but make it hit hard like a track that bumps in the streets. Try to use slang like "yo yo", "gimmie", and "pull up".
            You need to use a lot of it. Imagine you are the voice of youth.
            """;

        var contextSecond = await AIHub.Agent()
            .WithBackend(BackendType.OpenAi)
            .WithModel("gpt-4o")
            .WithInitialPrompt(systemPromptSecond)
            .CreateAsync(interactiveResponse: true);
        
        var context = await AIHub.Agent()
            .WithModel("gemma2:2b")
            .WithInitialPrompt(systemPrompt)
            .WithSteps(StepBuilder.Instance
                .Answer()
                .Redirect(agentId: contextSecond.GetAgentId())
                .Build())
            .CreateAsync();
        
        await context
            .ProcessAsync("Write a poem about distant future");

    }
}