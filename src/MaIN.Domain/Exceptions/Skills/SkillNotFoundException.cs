using System.Net;
using MaIN.Domain.Exceptions;

namespace MaIN.Domain.Exceptions.Skills;

public class SkillNotFoundException(string skillName)
    : MaINCustomException($"Skill '{skillName}' was not found in the registry.")
{
    public override string PublicErrorMessage => "Skill not found.";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.NotFound;
}
