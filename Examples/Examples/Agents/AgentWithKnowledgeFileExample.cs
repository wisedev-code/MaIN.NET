using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;

namespace Examples.Agents;

public class AgentWithKnowledgeFileExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Agent with knowledge base example");
        AIHub.Extensions.DisableLLamaLogs();
        var context = await AIHub.Agent()
            .WithModel("gemma3:4b")
            .WithInitialPrompt("""
                               You are a helpful assistant that answers questions about a company. Try to
                                help employees find answers to their questions. Company you work for is TechVibe Solutions.
                               """)
            .WithKnowledge(KnowledgeBuilder.Instance
                .AddFile("people.md", "./Files/Knowledge/people.md", 
                    tags: ["workers", "employees", "company"])
                .AddFile("organization.md", "./Files/Knowledge/organization.md",
                    tags:["company structure", "company policy", "company culture", "company overview"])
                .AddFile("events.md", "./Files/Knowledge/events.md",
                    tags: ["company events", "company calendar", "company agenda"])
                .AddFile("office_layout.md", "./Files/Knowledge/office_layout.md",
                    tags: ["company layout", "company facilities", "company environment", "office items", "supplies"]))
            .WithSteps(StepBuilder.Instance
                .AnswerUseKnowledge()
                .Build())
            .CreateAsync();
        
        var result = await context
            .ProcessAsync("Hey! Where I can find some printer paper?");
        Console.WriteLine(result.Message.Content);;

    }
}