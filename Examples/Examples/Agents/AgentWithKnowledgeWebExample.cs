using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;
using MaIN.Domain.Entities.Agents.AgentSource;
using Microsoft.Identity.Client;

namespace Examples.Agents;

public class AgentWithKnowledgeWebExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Vacation Planning Assistant with Specialized Travel Knowledge");

        var context = await AIHub.Agent()
            .WithModel("llama3.1:8b")
            .WithInitialPrompt("""
                               You are an expert vacation planner and travel consultant. Help users plan their
                               perfect trips by providing destination recommendations, itinerary suggestions,
                               budget planning advice, and practical travel tips. Focus on creating memorable
                               experiences tailored to each traveler's preferences and needs.
                               """)
            .WithKnowledge(KnowledgeBuilder.Instance
                .AddUrl("skyscanner_tips", "https://www.skyscanner.com/tips-and-inspiration/",
                    tags: ["flight deals", "travel hacks", "booking tips", "cheap flights"])
                .AddUrl("japan_guide", "https://www.japan-guide.com/e/e2025.html",
                    tags: ["japan travel", "itineraries", "cultural tips", "transportation"])
                .AddUrl("iceland_complete", "https://www.icelandcomplete.com/ring-road-itinerary",
                    tags: ["iceland travel", "ring road", "natural attractions", "road trip planning"])
                .AddUrl("rome2rio_routes", "https://www.rome2rio.com",
                    tags: ["transportation options", "route planning", "multi-modal travel"])
                .AddUrl("hostelworld_guides", "https://www.hostelworld.com/blog/",
                    tags: ["budget accommodation", "backpacking", "hostel reviews", "solo travel"])
                .AddUrl("airbnb_magazine", "https://www.airbnb.com/resources/hosting-homes/",
                    tags: ["unique stays", "local experiences", "vacation rentals"])
                .AddUrl("viator_tours", "https://www.viator.com/tours/",
                    tags: ["guided tours", "day trips", "activities", "experiences"])
                .AddUrl("lonely_planet_experiences", "https://www.lonelyplanet.com/experiences",
                    tags: ["local experiences", "cultural activities", "adventure travel"])
                .AddUrl("travel_state_gov", "https://travel.state.gov/content/travel/en/traveladvisories/",
                    tags: ["travel advisories", "safety information", "visa requirements"])
                .AddUrl("seat61_trains", "https://www.seat61.com",
                    tags: ["train travel", "rail routes", "europe by rail", "train tickets"]))
            .WithSteps(StepBuilder.Instance
                .AnswerUseKnowledge()
                .Build())
            .CreateAsync();
        

        // Budget planning
        var budgetResult = await context
            .ProcessAsync("How much should I budget per day for food and activities in Iceland?");
        Console.WriteLine(budgetResult.Message.Content);
    }
}