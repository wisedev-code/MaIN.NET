using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Domain.Entities.Skills;

namespace MaIN.Core.Hub.Skills;

public class WebSearchSkillProvider(string url = "https://feeds.bbci.co.uk/news/rss.xml") : IAgentSkillProvider
{
    public AgentSkill GetSkill() => new()
    {
        Name = "web-search",
        Description = "Fetches content from a web URL and makes it available for answering.",
        Tags = ["web", "fetch", "data"],
        Priority = 10,
        StepPlacement = SkillStepPlacement.Before,
        Steps = ["FETCH_DATA"],
        Source = new SkillSourceDefinition
        {
            Details = new AgentWebSourceDetails { Url = url },
            Type = AgentSourceType.Web
        }
    };
}
