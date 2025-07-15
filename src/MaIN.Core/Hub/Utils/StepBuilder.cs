using MaIN.Domain.Entities.Agents.Knowledge;
using MaIN.Services.Services.Models.Commands;

namespace MaIN.Core.Hub.Utils;

public class StepBuilder
{
    private readonly List<string> Steps = new();
    public static StepBuilder Instance => new();

    public StepBuilder Answer()
    {
        Steps.Add("ANSWER");
        return this;
    }

    public StepBuilder AnswerUseMemory()
    {
        Steps.Add("ANSWER+USE_MEMORY");
        return this;
    }

    public StepBuilder AnswerUseKnowledge()
    {
        Steps.Add("ANSWER+USE_KNOWLEDGE");
        return this;
    }

    public StepBuilder AnswerUseKnowledgeWithTags(params string[] tags)
    {
        var tagsString = string.Join(",", tags);
        Steps.Add($"ANSWER+USE_KNOWLEDGE+TAGS:{tagsString}");
        return this;
    }

    public StepBuilder AnswerUseKnowledgeWithType(KnowledgeItemType type)
    {
        Steps.Add($"ANSWER+USE_KNOWLEDGE+TYPE:{type}");
        return this;
    }

    public StepBuilder AnswerUseKnowledgeAndMemory()
    {
        Steps.Add("ANSWER+USE_KNOWLEDGE+USE_MEMORY");
        return this;
    }

    public StepBuilder AnswerUseKnowledgeAndMemoryWithTags(params string[] tags)
    {
        var tagsString = string.Join(",", tags);
        Steps.Add($"ANSWER+USE_KNOWLEDGE+USE_MEMORY+TAGS:{tagsString}");
        return this;
    }

    public StepBuilder Become(string role)
    {
        Steps.Add($"BECOME+{role}");
        return this;
    }

    public StepBuilder FetchData(FetchResponseType fetchResponseType = FetchResponseType.AS_Answer)
    {
        var stepToAdd = fetchResponseType == FetchResponseType.AS_System ? "FETCH_DATA+AS_SYSTEM" : "FETCH_DATA";
        Steps.Add(stepToAdd);
        return this;
    }
    
    public StepBuilder Mcp()
    {
        var stepToAdd = "MCP";
        Steps.Add(stepToAdd);
        return this;
    }

    public StepBuilder Redirect(string agentId, string output = "AS_Output", string mode = "REPLACE")
    {
        Steps.Add($"REDIRECT+{agentId}+{output}+{mode}");
        return this;
    }

    public List<string> Build()
    {
        return Steps;
    }
}