namespace MaIN.Core.Hub.Utils;

public class StepBuilder
{
    public static List<string> Steps = new();
    public static StepBuilder Instance => new();

    public static StepBuilder Answer()
    {
        Steps.Add("ANSWER");
        return new StepBuilder();
    }

    public static StepBuilder AnswerUseMemory()
    {
        Steps.Add("ANSWER+USE_MEMORY");
        return new StepBuilder();
    }

    public static StepBuilder Become(string role)
    {
        Steps.Add($"BECOME+{role}");
        return new StepBuilder();
    }

    public static StepBuilder FetchData()
    {
        Steps.Add("FETCH_DATA");
        return new StepBuilder();
    }

    public static StepBuilder Redirect(Guid agentId, string output = "AS_Output", string mode = "REPLACE")
    {
        Steps.Add($"REDIRECT+{agentId}+{output}+{mode}");
        return new StepBuilder();
    }

    public static List<string> Build()
    {
        return Steps;
    }
}
