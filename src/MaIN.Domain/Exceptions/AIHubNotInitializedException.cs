using System.Net;

namespace MaIN.Domain.Exceptions;

public class AIHubNotInitializedException()
    : MaINCustomException("AIHub has not been initialized. Make sure to call 'AddAIHub' in your service configuration.")
{
    public override string PublicErrorMessage => LogMessage;
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Conflict;
}