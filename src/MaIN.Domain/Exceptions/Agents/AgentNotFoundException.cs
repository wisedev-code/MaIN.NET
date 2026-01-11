using System.Net;

namespace MaIN.Domain.Exceptions.Agents;

public class AgentNotFoundException(string agentId) 
    : MaINCustomException($"Agent with id: '{agentId}' not found.")
{
    public override string PublicErrorMessage => "Agent not found.";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.NotFound;
}