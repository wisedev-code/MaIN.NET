using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;

namespace Examples.Agents;

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