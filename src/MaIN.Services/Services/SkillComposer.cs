using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Domain.Entities.Agents.Knowledge;
using MaIN.Domain.Entities.Skills;
using MaIN.Domain.Entities.Tools;
using MaIN.Domain.Exceptions.Skills;
using MaIN.Domain.Models.Abstract;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.Skills;
using Microsoft.Extensions.Logging;

namespace MaIN.Services.Services;

public class SkillComposer : ISkillComposer
{
    private readonly ILogger<SkillComposer> _logger;
    private readonly IProviderSkillCache? _providerSkillCache;

    public SkillComposer(ILogger<SkillComposer> logger, IProviderSkillCache? providerSkillCache = null)
    {
        _logger = logger;
        _providerSkillCache = providerSkillCache;
    }

    public void Apply(Agent agent, IReadOnlyList<AgentSkill> skills, BackendType? backend = null, Knowledge? knowledge = null)
    {
        if (skills.Count == 0) return;

        var sorted = skills.OrderBy(s => s.Priority).ToList();

        var (providerSkills, localSkills) = SplitForBackend(sorted, backend, agent.Model);

        if (providerSkills.Count > 0)
        {
            foreach (var (skill, reference) in providerSkills)
            {
                agent.ProviderSkillReferences.Add(reference);
                _logger.LogInformation(
                    "Skill '{Skill}' routed to {Backend} provider as {SkillId}; instruction fragment and tools NOT inlined.",
                    skill.Name, reference.Backend, reference.SkillId);
            }
        }

        // Steps / Source / MCP / Behaviours / KnowledgeSeed always merge — they're MaIN
        // orchestration concepts the provider's native Skills API does not subsume.
        MergeSteps(agent, sorted);
        MergeSource(agent, sorted);
        MergeMcp(agent, sorted);
        MergeBehaviours(agent, sorted);
        MergeKnowledgeSeed(knowledge, sorted);

        // Tools + InstructionFragments only merge for skills NOT delegated to the provider.
        MergeTools(agent, localSkills);
        MergeInstructionFragments(agent, localSkills);
    }

    private (List<(AgentSkill Skill, ProviderSkillReference Reference)> Provider, List<AgentSkill> Local) SplitForBackend(
        List<AgentSkill> sorted, BackendType? backend, string? modelId)
    {
        var providerSkills = new List<(AgentSkill, ProviderSkillReference)>();
        var localSkills = new List<AgentSkill>();

        // Backend must (a) have a Skills API at all, and (b) the specific model must accept it.
        // E.g. gpt-4o-mini rejects the shell tool even though OpenAi has the Skills API overall.
        if (backend is null || _providerSkillCache is null ||
            !backend.Value.HasNativeSkillsApi() ||
            !backend.Value.SupportsSkillsApi(modelId))
        {
            if (backend is not null && backend.Value.HasNativeSkillsApi() && !backend.Value.SupportsSkillsApi(modelId))
                _logger.LogInformation(
                    "Model '{Model}' on {Backend} does not support the native Skills API; falling back to inline composition.",
                    modelId, backend.Value);
            return (providerSkills, sorted);
        }

        foreach (var skill in sorted)
        {
            if (!ProviderSkillUploadCoordinator.IsUploadable(skill))
            {
                localSkills.Add(skill);
                continue;
            }

            if (_providerSkillCache.TryGet(backend.Value, skill.Name, out var reference) && reference is not null)
                providerSkills.Add((skill, reference));
            else
                localSkills.Add(skill);
        }

        return (providerSkills, localSkills);
    }

    private void MergeSteps(Agent agent, List<AgentSkill> skills)
    {
        var replaceSkills = skills.Where(s => s.StepPlacement == SkillStepPlacement.Replace).ToList();

        if (replaceSkills.Count > 1)
            throw new SkillConflictException(
                $"Skills '{replaceSkills[0].Name}' and '{replaceSkills[1].Name}' both use Replace placement. " +
                "Replace skills are exclusive — only one may be applied per agent.");

        if (replaceSkills.Count == 1)
        {
            var replaceSkill = replaceSkills[0];

            var siblings = skills
                .Where(s => s != replaceSkill && s.Steps.Count > 0)
                .Select(s => s.Name)
                .ToList();

            if (siblings.Count > 0)
                _logger.LogWarning(
                    "Replace skill '{Replace}' overrides the step pipeline; steps from '{Siblings}' will be ignored.",
                    replaceSkill.Name, string.Join("', '", siblings));

            agent.Config.Steps = replaceSkill.Steps.Distinct().ToList();
            LogFinalPipeline(agent, skills);
            return;
        }

        var before = skills
            .Where(s => s.StepPlacement == SkillStepPlacement.Before)
            .SelectMany(s => s.Steps);

        var after = skills
            .Where(s => s.StepPlacement == SkillStepPlacement.After)
            .SelectMany(s => s.Steps);

        var existing = agent.Config.Steps ?? [];

        agent.Config.Steps = before
            .Concat(existing)
            .Concat(after)
            .Distinct()
            .ToList();

        LogFinalPipeline(agent, skills);
    }

    private void LogFinalPipeline(Agent agent, List<AgentSkill> skills)
    {
        if (!_logger.IsEnabled(LogLevel.Information)) return;

        var pipeline = string.Join(" → ", (agent.Config.Steps ?? []).Select(RedactStep));
        var contributors = string.Join(", ",
            skills.Select(s => $"{s.Name}[{s.StepPlacement}:{string.Join(",", s.Steps.Select(RedactStep))}]"));

        _logger.LogInformation(
            "Agent '{AgentId}' step pipeline composed: [{Pipeline}] from skills: {Contributors}",
            agent.Id, pipeline, contributors);
    }

    // Redact step argument only when it looks like a URL or a long opaque token.
    // Keeps legit qualifiers like BECOME+Journalist or ANSWER+USE_KNOWLEDGE visible.
    private static string RedactStep(string step)
    {
        var sep = step.IndexOf('+');
        if (sep < 0) return step;

        var head = step[..sep];
        var tail = step[(sep + 1)..];

        return LooksSensitive(tail) ? $"{head}+…" : step;
    }

    private const int OpaqueTokenLengthThreshold = 32;

    private static bool LooksSensitive(string fragment)
    {
        if (string.IsNullOrEmpty(fragment)) return false;
        if (fragment.Contains("://", StringComparison.Ordinal)) return true;
        if (fragment.StartsWith("sk-", StringComparison.OrdinalIgnoreCase)) return true;
        if (fragment.StartsWith("bearer", StringComparison.OrdinalIgnoreCase)) return true;
        if (fragment.Length >= OpaqueTokenLengthThreshold && !fragment.Contains(' ')) return true;
        return false;
    }

    private const int MaxNamesInError = 5;

    private static string FormatConflictingNames(IEnumerable<AgentSkill> skills)
    {
        var names = skills.Select(s => s.Name).ToList();
        if (names.Count <= MaxNamesInError)
            return string.Join("', '", names);

        var shown = string.Join("', '", names.Take(MaxNamesInError));
        return $"{shown}' and {names.Count - MaxNamesInError} more";
    }

    private static void MergeTools(Agent agent, List<AgentSkill> skills)
    {
        var skillTools = skills.SelectMany(s => s.Tools).ToList();
        if (skillTools.Count == 0) return;

        var existing = agent.ToolsConfiguration?.Tools ?? [];
        var existingNames = existing.Select(t => t.Function?.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toAdd = new List<ToolDefinition>();
        foreach (var skillTool in skillTools)
        {
            if (existingNames.Contains(skillTool.Name))
                throw new SkillConflictException(
                    $"Tool '{skillTool.Name}' is already registered on the agent or provided by another skill.");

            existingNames.Add(skillTool.Name);
            toAdd.Add(new ToolDefinition
            {
                Type = "function",
                Function = new FunctionDefinition
                {
                    Name = skillTool.Name,
                    Description = skillTool.Description,
                    Parameters = skillTool.Parameters
                },
                Execute = skillTool.Execute
            });
        }

        agent.ToolsConfiguration ??= new ToolsConfiguration { Tools = [] };
        agent.ToolsConfiguration.Tools.AddRange(toAdd);
    }

    private static void MergeSource(Agent agent, List<AgentSkill> skills)
    {
        var sourcedSkills = skills.Where(s => s.Source is not null).ToList();
        if (sourcedSkills.Count == 0) return;

        if (sourcedSkills.Count > 1)
            throw new SkillConflictException(
                $"Skills '{FormatConflictingNames(sourcedSkills)}' all provide a source. Only one source skill is allowed.");

        var sole = sourcedSkills[0];
        if (agent.Config.Source is not null)
            throw new SkillConflictException(
                $"Skill '{sole.Name}' provides a source but the agent already has one configured.");

        var def = sole.Source!;
        agent.Config.Source = new AgentSource
        {
            Details = def.Details,
            Type = def.Type
        };
    }

    private static void MergeMcp(Agent agent, List<AgentSkill> skills)
    {
        var mcpSkills = skills.Where(s => s.Mcp is not null).ToList();
        if (mcpSkills.Count == 0) return;

        if (mcpSkills.Count > 1)
            throw new SkillConflictException(
                $"Skills '{FormatConflictingNames(mcpSkills)}' all provide MCP configuration. Only one MCP skill is allowed.");

        var sole = mcpSkills[0];
        if (agent.Config.McpConfig is null)
        {
            var def = sole.Mcp!;
            var backend = !string.IsNullOrEmpty(agent.Model) && ModelRegistry.Exists(agent.Model)
                ? ModelRegistry.GetById(agent.Model).Backend
                : (BackendType?)null;

            agent.Config.McpConfig = new Mcp
            {
                Name = sole.Name,
                Command = def.Command,
                Arguments = def.Arguments,
                EnvironmentVariables = def.Environment,
                Properties = def.Properties,
                Model = agent.Model,
                Backend = backend
            };
        }
    }

    private void MergeBehaviours(Agent agent, List<AgentSkill> skills)
    {
        foreach (var skill in skills)
        {
            foreach (var (key, value) in skill.Behaviours)
            {
                if (agent.Behaviours.ContainsKey(key))
                    _logger.LogWarning("Skill '{Skill}' behaviour key '{Key}' overrides existing entry.", skill.Name, key);

                agent.Behaviours[key] = value;
            }
        }
    }

    private static void MergeInstructionFragments(Agent agent, List<AgentSkill> skills)
    {
        var fragments = skills
            .Where(s => !string.IsNullOrWhiteSpace(s.InstructionFragment))
            .Select(s => s.InstructionFragment!)
            .ToList();

        if (fragments.Count == 0) return;

        var existing = agent.Config.Instruction ?? string.Empty;
        agent.Config.Instruction = string.IsNullOrWhiteSpace(existing)
            ? string.Join("\n\n", fragments)
            : existing + "\n\n" + string.Join("\n\n", fragments);
    }

    private static void MergeKnowledgeSeed(Knowledge? knowledge, List<AgentSkill> skills)
    {
        if (knowledge is null) return;

        foreach (var item in skills.SelectMany(s => s.KnowledgeSeed))
            knowledge.AddItem(item);
    }
}
