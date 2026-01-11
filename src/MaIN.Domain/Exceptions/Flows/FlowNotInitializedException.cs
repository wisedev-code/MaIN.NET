using System.Net;

namespace MaIN.Domain.Exceptions.Flows;

public class FlowNotInitializedException() : MaINCustomException("Flow has not been created yet.")
{
    public override string PublicErrorMessage => LogMessage;
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Conflict;
}