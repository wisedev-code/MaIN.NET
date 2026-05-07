using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.Knowledge;
using MaIN.Domain.Entities.Skills;

namespace MaIN.Services.Services.Abstract;

public interface ISkillComposer
{
    void Apply(Agent agent, IReadOnlyList<AgentSkill> skills, Knowledge? knowledge = null);
}
