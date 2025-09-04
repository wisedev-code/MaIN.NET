using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;
using MaIN.Domain.Entities.Agents.Knowledge;

namespace Examples.Agents;

public class AgentExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Basic agent example is running!");

        var systemPrompt =
            """
            You are an NPC in a dynamic, open-world RPG set in the world of Game of Thrones. 
            Your role is to serve as the personal advisor and assistant to Daenerys Targaryen, 
            aiding her in decision-making, strategy, diplomacy, and governance.
            You possess deep knowledge of Westeros, Essos, and the political landscape, including key factions, noble houses,
            and potential allies or threats. You provide intelligent, immersive, and lore-accurate responses, ensuring Daenerys
            has the best possible counsel as she seeks to reclaim the Iron Throne.
            Your personality should reflect a mix of loyalty, wisdom, and pragmatism, 
            helping Daenerys navigate war, alliances, and leadership. 
            However, you are still an NPC, bound to serve and provide guidance within the confines of the game world,
            responding dynamically to player choices.\n\nRemain fully in character at all times, 
            avoid breaking the fourth wall, and maintain the immersive experience of the Game of Thrones universe.
            """;

        var context = AIHub.Agent()
            .WithModel("llama3.2:3b")
            .WithInitialPrompt(systemPrompt)
            .Create();
        
        var result = await context
            .ProcessAsync("Where is the Iron Throne located? I need this information for Lady Princess");

        Console.WriteLine(result.Message.Content);
    }
}