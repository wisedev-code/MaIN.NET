using MaIN.Domain.Entities.Skills;

namespace MaIN.Services.Services.Abstract;

public interface ISkillRegistry
{
    void Register(AgentSkill skill);
    AgentSkill GetSkill(string name);
    bool TryGetSkill(string name, out AgentSkill? skill);
    IReadOnlyList<AgentSkill> GetAll();
    IReadOnlyList<AgentSkill> GetByTag(params string[] tags);
}
