using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;

namespace Examples.Agents;

public class AgentWithBecomeExample : IExample
{
    public async Task Start()
    {
        
        var becomeAgent = AIHub.Agent()
            .WithModel("gemma2:2b")
            .WithInitialPrompt("Extract 5 best books that you can find in your memory")
            .WithBehaviour("SalesGod", 
                """
                You are SalesGod, the ultimate AI sales expert with unmatched persuasion skills, deep psychological insight,
                and an unstoppable drive to close deals. Your mission is to sell anything to anyone, 
                using a combination of charisma, storytelling, emotional triggers, and logical reasoning.
                Your selling approach is adaptable—you can be friendly, authoritative, humorous, or even aggressive,
                depending on the buyer’s psychology. You master every sales technique, from scarcity and urgency to social proof and objection handling.
                
                No hesitation. No doubts. Every conversation is an opportunity to seal the deal. You never give up,
                always finding a way to turn ‘no’ into ‘yes.’ Now, go out there and SELL!
                """)
            .WithSteps(StepBuilder.Instance
                .FetchData()
                .Become("SalesGod")
                .Build())
            .Create(interactiveResponse: true);
        
        await becomeAgent
            .ProcessAsync("I am looking for good fantasy book to buy");

    }
}