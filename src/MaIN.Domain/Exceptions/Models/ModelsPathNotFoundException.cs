using System.Net;

namespace MaIN.Domain.Exceptions.Models;

public class ModelsPathNotFoundException() : MaINCustomException($"Models path not found in configuration or environment variables.")
{
    public override string PublicErrorMessage => Message;
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.NotFound;
}