using MaIN.Domain.Entities.Agents.Knowledge;

namespace MaIN.Domain.Entities.Skills;

public class AgentSkill
{
    /// <summary>Unique skill identifier used in WithSkill("name"). e.g. "web-search", "calculator"</summary>
    public required string Name { get; init; }

    /// <summary>Human-readable description shown in registries and tooling. e.g. "Fetches and summarizes web content"</summary>
    public string? Description { get; init; }

    /// <summary>Semver string, informational only. e.g. "1.0.0"</summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>
    /// Pipeline steps contributed by this skill. e.g. ["FETCH_DATA", "ANSWER"] or ["BECOME+Journalist", "ANSWER"].
    /// How they merge with the agent's own steps depends on <see cref="StepPlacement"/>.
    /// </summary>
    public List<string> Steps { get; init; } = [];

    /// <summary>
    /// C# function-backed tools injected into the agent's tool registry.
    /// Each tool needs a name, JSON schema for parameters, and an Execute delegate.
    /// Cannot be defined in .md file-based skills — requires IAgentSkillProvider.
    /// </summary>
    public List<SkillToolDefinition> Tools { get; init; } = [];

    /// <summary>
    /// Data source wired to the agent (web page, file, API, etc.).
    /// AgentConfig.Source is a single property (not a list), so only one source per agent is supported
    /// anywhere in the system — not just via skills. Second skill with a source throws SkillConflictException.
    /// </summary>
    public SkillSourceDefinition? Source { get; init; }

    /// <summary>
    /// MCP server configuration injected into the agent.
    /// e.g. npx @modelcontextprotocol/server-filesystem with allowed directories.
    /// Model is inherited from the agent at compose time.
    /// </summary>
    public SkillMcpDefinition? Mcp { get; init; }

    /// <summary>
    /// Named persona definitions merged into the agent's behaviour map.
    /// e.g. { "Journalist": "Write concise newsletters based on the data provided." }
    /// Used with BECOME+Name steps.
    /// </summary>
    public Dictionary<string, string> Behaviours { get; init; } = [];

    /// <summary>
    /// Knowledge items pre-seeded into the agent's knowledge index at creation time.
    /// Additive — multiple skills can each contribute knowledge without conflict.
    /// </summary>
    public List<KnowledgeIndexItem> KnowledgeSeed { get; init; } = [];

    /// <summary>
    /// Text appended to the agent's system prompt (Config.Instruction).
    /// In .md skills this is the Markdown body below the YAML frontmatter.
    /// Multiple skills' fragments are concatenated in Priority order.
    /// </summary>
    public string? InstructionFragment { get; init; }

    /// <summary>Categorisation tags for filtering. e.g. ["web", "search"] or ["math", "tools"]</summary>
    public string[] Tags { get; init; } = [];

    /// <summary>
    /// Merge order when multiple skills are applied. Lower = applied first.
    /// Built-in skills use 5–80; user skills default to 100.
    /// Affects step ordering and which InstructionFragment appears first.
    /// </summary>
    public int Priority { get; init; } = 100;

    /// <summary>
    /// Controls where this skill's Steps are inserted relative to the agent's own steps.
    /// Before = prepend, After = append, Replace = discard agent steps entirely.
    /// </summary>
    public SkillStepPlacement StepPlacement { get; init; } = SkillStepPlacement.Before;
}
