using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;

namespace Examples.Agents.Flows;

public class AgentsComposedAsFlowExample : IExample
{
    /// <summary>
    /// To run this example uncomment SqliteSettings in appsettings.json as we need persistence for agents and chats
    /// </summary>
    public async Task Start()
    {
        Console.WriteLine("Basic agents flow example");

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

        var contextSecond = AIHub.Agent()
            .WithModel("gemma2:2b")
            .WithInitialPrompt(systemPromptSecond)
            .Create(interactiveResponse: true);
        
        var contextFirst = AIHub.Agent()
            .WithModel("llama3.2:3b")
            .WithInitialPrompt(systemPrompt)
            .WithSteps(StepBuilder.Instance
                .Answer()
                .Redirect(agentId: contextSecond.GetAgentId())
                .Build())
            .Create();

        var flowContext = AIHub.Flow()
            .WithName("PoetryAi")
            .WithDescription("Poem writing automated flow")
            .AddAgents([
                contextFirst.GetAgent(),
                contextSecond.GetAgent()
            ])
            .Save("./poetry.zip");
        
        await flowContext
            .ProcessAsync("Write a poem about distant future");

    }
}