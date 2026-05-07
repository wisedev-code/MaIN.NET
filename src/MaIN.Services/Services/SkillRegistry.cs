using MaIN.Domain.Entities.Skills;
using MaIN.Domain.Exceptions.Skills;
using MaIN.Services.Services.Abstract;

namespace MaIN.Services.Services;

public class SkillRegistry : ISkillRegistry
{
    private readonly Dictionary<string, AgentSkill> _skills =
        new(StringComparer.OrdinalIgnoreCase);

    public SkillRegistry(
        IEnumerable<IAgentSkillProvider> providers,
        IEnumerable<ISkillLoader> loaders)
    {
        foreach (var p in providers)
            Register(p.GetSkill());

        foreach (var l in loaders)
            foreach (var s in l.LoadAll())
                Register(s);
    }

    public void Register(AgentSkill skill) => _skills[skill.Name] = skill;

    public AgentSkill GetSkill(string name) =>
        _skills.TryGetValue(name, out var skill)
            ? skill
            : throw new SkillNotFoundException(name);

    public bool TryGetSkill(string name, out AgentSkill? skill) =>
        _skills.TryGetValue(name, out skill);

    public IReadOnlyList<AgentSkill> GetAll() =>
        _skills.Values.ToList().AsReadOnly();

    public IReadOnlyList<AgentSkill> GetByTag(params string[] tags) =>
        _skills.Values
            .Where(s => s.Tags.Any(t => tags.Contains(t, StringComparer.OrdinalIgnoreCase)))
            .ToList()
            .AsReadOnly();
}
