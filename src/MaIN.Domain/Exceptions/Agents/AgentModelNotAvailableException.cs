using System.Net;

namespace MaIN.Domain.Exceptions.Agents;

public class AgentModelNotAvailableException(string agentId, string modelId)
    : MaINCustomException($"Model '{modelId}' used by agent '{agentId}' is not registered. If this is a dynamically registered model, it must be re-registered after application restart.")
{
    public override string PublicErrorMessage => $"Model '{modelId}' is not available.";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.UnprocessableEntity;
}
