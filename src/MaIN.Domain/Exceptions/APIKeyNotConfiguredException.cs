using System.Net;

namespace MaIN.Domain.Exceptions;

public class APIKeyNotConfiguredException(string apiName) : MaINCustomException($"The API key of '{apiName}' has not been configured.")
{
    public override string PublicErrorMessage => "Agent not found.";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.InternalServerError;
}