using System.Net;

namespace MaIN.Domain.Exceptions;

public class AgentAlreadyExistsException(string agentId) 
    : MaINCustomException($"Agent with id: '{agentId}' already exists.")
{
    public override string PublicErrorMessage => "Agent already exists.";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Conflict;
}