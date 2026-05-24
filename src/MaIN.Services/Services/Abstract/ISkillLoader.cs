using MaIN.Domain.Entities.Skills;

namespace MaIN.Services.Services.Abstract;

public interface ISkillLoader
{
    IReadOnlyList<AgentSkill> LoadAll();
}
