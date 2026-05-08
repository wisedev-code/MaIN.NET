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
using Microsoft.Extensions.Logging;

namespace MaIN.Services.Services;

public class SkillComposer(ILogger<SkillComposer> logger) : ISkillComposer
{
    public void Apply(Agent agent, IReadOnlyList<AgentSkill> skills, Knowledge? knowledge = null)
    {
        if (skills.Count == 0) return;

        var sorted = skills.OrderBy(s => s.Priority).ToList();

        MergeSteps(agent, sorted);
        MergeTools(agent, sorted);
        MergeSource(agent, sorted);
        MergeMcp(agent, sorted);
        MergeBehaviours(agent, sorted);
        MergeInstructionFragments(agent, sorted);
        MergeKnowledgeSeed(knowledge, sorted);
    }

    private static void MergeSteps(Agent agent, List<AgentSkill> skills)
    {
        var replaceSkill = skills.LastOrDefault(s => s.StepPlacement == SkillStepPlacement.Replace);
        if (replaceSkill is not null)
        {
            agent.Config.Steps = replaceSkill.Steps.Distinct().ToList();
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
                $"Skills '{sourcedSkills[0].Name}' and '{sourcedSkills[1].Name}' both provide a source. Only one source skill is allowed.");

        if (agent.Config.Source is not null)
            throw new SkillConflictException(
                $"Skill '{sourcedSkills[0].Name}' provides a source but the agent already has one configured.");

        var def = sourcedSkills[0].Source!;
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
                $"Skills '{mcpSkills[0].Name}' and '{mcpSkills[1].Name}' both provide MCP configuration. Only one MCP skill is allowed.");

        if (agent.Config.McpConfig is null)
        {
            var def = mcpSkills[0].Mcp!;
            var backend = !string.IsNullOrEmpty(agent.Model) && ModelRegistry.Exists(agent.Model)
                ? ModelRegistry.GetById(agent.Model).Backend
                : (BackendType?)null;

            agent.Config.McpConfig = new Mcp
            {
                Name = mcpSkills[0].Name,
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
                    logger.LogWarning("Skill '{Skill}' behaviour key '{Key}' overrides existing entry.", skill.Name, key);

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
