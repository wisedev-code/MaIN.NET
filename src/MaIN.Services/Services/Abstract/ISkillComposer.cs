using MaIN.Domain.Configuration;
using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Entities.Agents.Knowledge;
using MaIN.Domain.Entities.Skills;

namespace MaIN.Services.Services.Abstract;

public interface ISkillComposer
{
    /// <summary>
    /// Compose skills into the agent. When <paramref name="backend"/> is set and points at a
    /// cloud provider with native Skills API support, uploadable skills are added as
    /// <see cref="Agent.ProviderSkillReferences"/> instead of being inlined as tools/instructions.
    /// </summary>
    void Apply(Agent agent, IReadOnlyList<AgentSkill> skills, BackendType? backend = null, Knowledge? knowledge = null);
}
