using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;
using MaIN.Domain.Entities.Agents.AgentSource;

namespace Examples.Agents;

public class AgentWithApiDataSourceExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Agent with api source");
        
        var context = AIHub.Agent()
            .WithModel("llama3.2:3b")
            .WithInitialPrompt("Extract at least 4 jobs offers (try to include title, company name, salary and location if possible)")
            .WithSource(new AgentApiSourceDetails()
            {
                Method = "Get",
                Query = "https://remoteok.com/api?tags=javascript",
                ResponseType = "JSON"
            }, AgentSourceType.API)
            .WithSteps(StepBuilder.Instance
                .FetchData()
                .Answer()
                .Build())
            .Create(interactiveResponse: true);
        
        await context
            .ProcessAsync("I am looking for work as javascript developer");

    }
}