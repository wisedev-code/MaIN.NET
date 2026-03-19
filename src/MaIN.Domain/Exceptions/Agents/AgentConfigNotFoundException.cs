using System.Net;

namespace MaIN.Domain.Exceptions.Agents;

public class AgentConfigNotFoundException(string agentId) : MaINCustomException($"Config of the agent with id: '{agentId}' not found.")
{
    public override string PublicErrorMessage => "Agent config not found.";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.NotFound;
}
