using System.Net;

namespace MaIN.Domain.Exceptions.MPC;

public class MPCConfigNotFoundException() : MaINCustomException("MPC configuration not found.")
{
    public override string PublicErrorMessage => LogMessage;
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.NotFound;
}