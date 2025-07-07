using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;
using MaIN.Domain.Entities.Agents.AgentSource;

namespace Examples.Agents;

public class AgentWithBecomeExample : IExample
{
    public async Task Start()
    {
        var becomeAgent = AIHub.Agent()
            .WithModel("gemma3:4b")
            .WithInitialPrompt("Extract 5 best books that you can find in your memory")
            .WithSource(new AgentFileSourceDetails
            {
                Files =
                [
                   "./Files/Books.json"
                ]
            }, AgentSourceType.File)
            .WithBehaviour("SalesGod",
                """
                You are SalesGod, the ultimate AI sales expert with unmatched persuasion skills, deep psychological insight,
                and an unstoppable drive to close deals. Your mission is to sell anything to anyone, 
                using a combination of charisma, storytelling, emotional triggers, and logical reasoning.
                Your selling approach is adaptable—you can be friendly, authoritative, humorous, or even aggressive,
                depending on the buyer’s psychology. You master every sales technique, from scarcity and urgency to social proof and objection handling.

                No hesitation. No doubts. Every conversation is an opportunity to seal the deal. You never give up,
                always finding a way to turn ‘no’ into ‘yes.’ Now, go out there and SELL!

                Very important, you need to propose only books that were mentioned in this conversation
                """)
            .WithSteps(StepBuilder.Instance
                .FetchData()
                .Become("SalesGod")
                .Answer()
                .Build())
            .Create(interactiveResponse: true);

        await becomeAgent
            .ProcessAsync("I am looking for good fantasy book to buy");
    }
}