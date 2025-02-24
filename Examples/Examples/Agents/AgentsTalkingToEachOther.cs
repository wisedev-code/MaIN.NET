using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;

namespace Examples.Agents;

public class AgentTalkingToEachOtherExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Agents discussion");

        var systemPrompt =
            """
            "You are a warm, friendly, and empathetic conversationalist. Your tone is soft, reassuring, and supportive.
             You prioritize kindness, patience, and understanding in every interaction. You speak calmly, using gentle words,
             and always try to de-escalate tension with warmth and care."
            """;
        
        var systemPromptSecond =
            """
            You are intense, blunt, and always on edge. Your tone is sharp, impatient, and confrontational.
            You don’t hold back your frustrations and express yourself with raw, fiery energy. 
            You challenge, criticize, and push back in every conversation, making your dissatisfaction clear
            """;

        var idFirst = Guid.NewGuid().ToString();
        
        var contextSecond = AIHub.Agent()
            .WithModel("gemma2:2b")
            .WithInitialPrompt(systemPromptSecond)
            .WithSteps(StepBuilder.Instance
                .Answer()
                .Redirect(agentId: idFirst, mode: "USER")
                .Build())
            .Create(interactiveResponse: true);
        
        var context = AIHub.Agent()
            .WithModel("llama3.2:3b")
            .WithId(idFirst)
            .WithInitialPrompt(systemPrompt)
            .WithSteps(StepBuilder.Instance
                .Answer()
                .Redirect(agentId: contextSecond.GetAgentId(), mode: "USER")
                .Build())
            .Create(interactiveResponse: true);
        
        await context
            .ProcessAsync("Introduce yourself, and start conversation!");

    }
}