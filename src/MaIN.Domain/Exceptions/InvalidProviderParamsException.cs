using System.Net;

namespace MaIN.Domain.Exceptions;

public class InvalidProviderParamsException(string serviceName, string expectedType, string receivedType)
    : MaINCustomException($"{serviceName} service requires {expectedType}, but received {receivedType}.")
{
    public override string PublicErrorMessage => Message;
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;
}
