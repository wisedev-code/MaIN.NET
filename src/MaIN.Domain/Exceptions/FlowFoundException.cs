using System.Net;

namespace MaIN.Domain.Exceptions;

public class FlowFoundException(string flowId)
    : MaINCustomException($"Flow with id: '{flowId}' not found.")
{
    public override string PublicErrorMessage => "Flow not found.";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.NotFound;
}