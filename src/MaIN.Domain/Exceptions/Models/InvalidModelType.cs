using System.Net;

namespace MaIN.Domain.Exceptions.Models;

public class InvalidModelTypeException(string expectedType)
    : MaINCustomException($"Expected {expectedType}")
{
    public override string PublicErrorMessage => "Invalid model type.";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;
}
