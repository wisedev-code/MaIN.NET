using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;
using MaIN.Domain.Entities;
using FileInfo = MaIN.Domain.Entities.FileInfo;

namespace Examples.Agents;

public class AgentWithRedirectImageExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Basic agent&friends with images example is running!");

        var systemPrompt =
            """
            You analyze a stored PDF and generate an image prompt. Your output must be a single prompt with a maximum of 10 words.
            Do not include any explanations, context, or extra text—only the prompt itself.
            Avoid mentioning specific characters or names; focus on the topic and context.
            """;
        
        var systemPromptSecond =
            """
            Generate image based on given prompt
            """;

        var contextSecond = AIHub.Agent()
            .WithModel("FLUX.1_Shnell")
            .WithInitialPrompt(systemPromptSecond)
            .Create();
        
        var context = AIHub.Agent()
            .WithModel("llama3.1:8b")
            .WithInitialPrompt(systemPrompt)
            .WithSteps(StepBuilder.Instance
                .Answer()
                .Redirect(agentId: contextSecond.GetAgentId())
                .Build())
            .Create(interactiveResponse: true);
        
        await context
            .ProcessAsync(new Message()
            {
                Content = "Describe image based on document in your memory",
                Role = "User",
                Files = [new FileInfo()
                {
                    Name = "Nicolaus_Copernicus",
                    Extension = "pdf",
                    Path = "./Files/Nicolaus_Copernicus.pdf"
                }]
            });
    }
}