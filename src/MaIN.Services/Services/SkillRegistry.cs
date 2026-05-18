using System.Collections.Concurrent;
using MaIN.Domain.Entities.Skills;
using MaIN.Domain.Exceptions.Skills;
using MaIN.Services.Services.Abstract;
using Microsoft.Extensions.Logging;

namespace MaIN.Services.Services;

public class SkillRegistry : ISkillRegistry
{
    private readonly ConcurrentDictionary<string, AgentSkill> _skills =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, byte> _builtInNames =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ILogger<SkillRegistry> _logger;

    public SkillRegistry(
        IEnumerable<IAgentSkillProvider> providers,
        IEnumerable<ISkillLoader> loaders,
        ILogger<SkillRegistry> logger)
    {
        _logger = logger;

        foreach (var p in providers)
        {
            var skill = p.GetSkill();
            if (p is IBuiltInAgentSkillProvider)
                _builtInNames.TryAdd(skill.Name, 0);
            Register(skill);
        }

        foreach (var l in loaders)
            foreach (var s in l.LoadAll())
                Register(s);
    }

    public void Register(AgentSkill skill)
    {
        _skills.AddOrUpdate(
            skill.Name,
            _ => skill,
            (_, _) =>
            {
                _logger.LogWarning("Skill '{Name}' already registered — overwriting.", skill.Name);
                return skill;
            });
    }

    public AgentSkill GetSkill(string name) =>
        _skills.TryGetValue(name, out var skill)
            ? skill
            : throw new SkillNotFoundException(name);

    public bool TryGetSkill(string name, out AgentSkill? skill) =>
        _skills.TryGetValue(name, out skill);

    public IReadOnlyList<AgentSkill> GetAll() =>
        _skills.Values.ToList().AsReadOnly();

    public IReadOnlyList<AgentSkill> GetAllExcludingBuiltIn()
    {
        var builtInSnapshot = new HashSet<string>(_builtInNames.Keys, StringComparer.OrdinalIgnoreCase);
        return _skills.Values
            .Where(s => !builtInSnapshot.Contains(s.Name))
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<AgentSkill> GetByTag(params string[] tags) =>
        _skills.Values
            .Where(s => s.Tags.Any(t => tags.Contains(t, StringComparer.OrdinalIgnoreCase)))
            .ToList()
            .AsReadOnly();
}
