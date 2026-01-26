using System.Net;

namespace MaIN.Domain.Exceptions.Agents;

public class AgentFlowNotFoundException(string flowId) : MaINCustomException($"Agent flow with id: '{flowId}' not found.")
{
    public override string PublicErrorMessage => "Agent flow not found.";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.NotFound;
}