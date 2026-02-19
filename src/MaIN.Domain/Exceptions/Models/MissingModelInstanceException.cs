using System.Net;

namespace MaIN.Domain.Exceptions.Models;

public class MissingModelInstanceException() 
    : MaINCustomException("Model instance cannot be null.")
{
    public override string PublicErrorMessage => Message;
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;
}
