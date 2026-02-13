using System.Net;

namespace MaIN.Domain.Exceptions.Models;

public class MissingModelIdException(string modelIdParameter) 
    : MaINCustomException($"Model id cannot be null or empty, {modelIdParameter}.")
{
    public override string PublicErrorMessage => "Model name cannot be null or empty";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;
}
