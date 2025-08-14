using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents.AgentSource;
using Microsoft.Identity.Client;

namespace Examples.Agents;

public class AgentWithKnowledgeMcpExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("");

        var context = await AIHub.Agent()
            .WithKnowledge(KnowledgeBuilder.Instance
                .AddMcp("name", new Mcp()
                {
                    
                }))
            .WithSteps(StepBuilder.Instance
                .AnswerUseKnowledge()
                .Build())
            .CreateAsync();

        var result = await context
            .ProcessAsync("");

        Console.WriteLine(result.Message.Content);
    }
}