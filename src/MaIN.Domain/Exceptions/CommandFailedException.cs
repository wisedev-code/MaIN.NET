using System.Net;

namespace MaIN.Domain.Exceptions;

public class CommandFailedException(string commandName) 
    : MaINCustomException($"{commandName} command execution failed.")
{
    public override string PublicErrorMessage => Message;
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.InternalServerError;
}