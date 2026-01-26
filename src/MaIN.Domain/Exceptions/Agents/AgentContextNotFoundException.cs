using System.Net;

namespace MaIN.Domain.Exceptions.Agents;

public class AgentContextNotFoundException(string agentId) : MaINCustomException($"Context of agent with id: '{agentId}' not found.")
{
    public override string PublicErrorMessage => "Agent context not found.";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.NotFound;
}