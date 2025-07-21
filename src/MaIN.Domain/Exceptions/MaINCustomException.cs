using System.Net;

namespace MaIN.Domain.Exceptions;

public abstract class MaINCustomException(string message) : Exception(message)
{
    public string LogMessage { get; private set; } = message;
    public abstract string PublicErrorMessage { get; }
    public abstract HttpStatusCode HttpStatusCode { get; }
}