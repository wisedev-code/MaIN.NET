using System.Net;

namespace MaIN.Domain.Exceptions.Models;

public class MissingModelNameException(string modelNameParameter) 
    : MaINCustomException($"Model name cannot be null or empty, {modelNameParameter}.")
{
    public override string PublicErrorMessage => "Model name cannot be null or empty";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;
}