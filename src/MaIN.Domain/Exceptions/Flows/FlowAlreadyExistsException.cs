using System.Net;

namespace MaIN.Domain.Exceptions.Flows;

public class FlowAlreadyExistsException(string flowId) 
    : MaINCustomException($"Flow with id: '{flowId}' already exists.")
{
    public override string PublicErrorMessage => "Flow already exists.";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Conflict;
}