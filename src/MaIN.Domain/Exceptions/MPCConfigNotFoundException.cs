using System.Net;

namespace MaIN.Domain.Exceptions;

public class MPCConfigNotFoundException() : MaINCustomException("MPC configuration not found.")
{
    public override string PublicErrorMessage => LogMessage;
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.NotFound;
}