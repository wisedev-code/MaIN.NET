using System.Net;
using MaIN.Domain.Exceptions;

namespace MaIN.Domain.Exceptions.Skills;

public class SkillConflictException(string detail)
    : MaINCustomException($"Skill conflict: {detail}")
{
    public override string PublicErrorMessage => "Skill configuration conflict.";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Conflict;
}
