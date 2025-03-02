using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;
using MaIN.Domain.Entities.Agents.AgentSource;

namespace Examples.Agents;

public class AgentWithWebDataSourceOpenAiExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Agent with web source (OpenAi)");
        
        OpenAiExample.Setup(); //We need to provide OpenAi API key

        var context = await AIHub.Agent()
            .WithModel("gpt-4o-mini")
            .WithInitialPrompt("Find useful information about daily news, try to include title, description and link.")
            .WithBehaviour("Journalist", "Base on data provided in chat find useful information about what happen today. Build it in form of newsletter")
            .WithSource(new AgentWebSourceDetails()
            {
                Url = "https://www.bbc.com/",
            }, AgentSourceType.Web)
            .WithSteps(StepBuilder.Instance
                .FetchData()
                .Become("Journalist")
                .Answer()
                .Build())
            .CreateAsync(interactiveResponse: true);
        
        await context
            .ProcessAsync("Provide today's newsletter");

    }
}