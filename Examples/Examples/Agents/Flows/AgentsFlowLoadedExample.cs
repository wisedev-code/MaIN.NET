using MaIN.Core.Hub;

namespace Examples.Agents.Flows;

/// <summary>
/// To run this example uncomment SqliteSettings in appsettings.json as we need persistence for agents and chats
/// </summary>
public class AgentsFlowLoadedExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Basic agents flow example (loading)");

        var flowContext = AIHub.Flow()
            .Load("./poetry.zip");
        
        await flowContext
            .ProcessAsync("Write a poem about distant future");

    }
}