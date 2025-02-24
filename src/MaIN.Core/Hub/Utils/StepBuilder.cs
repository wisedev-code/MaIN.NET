namespace MaIN.Core.Hub.Utils;

public class StepBuilder
{
    public List<string> Steps = new();
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

    public StepBuilder Become(string role)
    {
        Steps.Add($"BECOME+{role}");
        return this;
    }

    public StepBuilder FetchData()
    {
        Steps.Add("FETCH_DATA");
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
