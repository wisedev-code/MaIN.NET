using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Domain.Entities.Agents.Knowledge;
using MaIN.Domain.Entities.Skills;

namespace MaIN.Core.Hub.Utils;

public class SkillBuilder
{
    private string _name = "";
    private string? _description;
    private string _version = "1.0.0";
    private readonly List<string> _steps = [];
    private readonly List<SkillToolDefinition> _tools = [];
    private SkillSourceDefinition? _source;
    private SkillMcpDefinition? _mcp;
    private readonly Dictionary<string, string> _behaviours = [];
    private readonly List<KnowledgeIndexItem> _knowledgeSeed = [];
    private string? _instructionFragment;
    private readonly List<string> _tags = [];
    private int _priority = 100;
    private SkillStepPlacement _placement = SkillStepPlacement.Before;

    private SkillBuilder() { }

    public static SkillBuilder Create(string name)
    {
        var builder = new SkillBuilder();
        builder._name = name;
        return builder;
    }

    public SkillBuilder WithDescription(string description) { _description = description; return this; }
    public SkillBuilder WithVersion(string version) { _version = version; return this; }
    public SkillBuilder WithSteps(params string[] steps) { _steps.AddRange(steps); return this; }
    public SkillBuilder WithPriority(int priority) { _priority = priority; return this; }
    public SkillBuilder WithPlacement(SkillStepPlacement placement) { _placement = placement; return this; }
    public SkillBuilder WithTags(params string[] tags) { _tags.AddRange(tags); return this; }
    public SkillBuilder WithInstructionFragment(string fragment) { _instructionFragment = fragment; return this; }
    public SkillBuilder WithBehaviour(string name, string instruction) { _behaviours[name] = instruction; return this; }
    public SkillBuilder WithTool(SkillToolDefinition tool) { _tools.Add(tool); return this; }
    public SkillBuilder WithKnowledgeSeed(KnowledgeIndexItem item) { _knowledgeSeed.Add(item); return this; }

    public SkillBuilder WithSource(IAgentSource source, AgentSourceType type)
    {
        _source = new SkillSourceDefinition { Details = source, Type = type };
        return this;
    }

    public SkillBuilder WithMcp(string command, List<string> arguments,
        Dictionary<string, string>? environment = null,
        Dictionary<string, string>? properties = null)
    {
        _mcp = new SkillMcpDefinition
        {
            Command = command,
            Arguments = arguments,
            Environment = environment ?? [],
            Properties = properties ?? []
        };
        return this;
    }

    public AgentSkill Build() => new()
    {
        Name = _name,
        Description = _description,
        Version = _version,
        Steps = [.._steps],
        Tools = [.._tools],
        Source = _source,
        Mcp = _mcp,
        Behaviours = new Dictionary<string, string>(_behaviours),
        KnowledgeSeed = [.._knowledgeSeed],
        InstructionFragment = _instructionFragment,
        Tags = [.._tags],
        Priority = _priority,
        StepPlacement = _placement
    };
}
