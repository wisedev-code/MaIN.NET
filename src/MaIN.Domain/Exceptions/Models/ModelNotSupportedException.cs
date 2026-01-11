using System.Net;

namespace MaIN.Domain.Exceptions.Models;

public class ModelNotSupportedException(string? modelName) 
    : MaINCustomException($"Given model {modelName ?? string.Empty} is not supported.")
{
    public override string PublicErrorMessage => Message;
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;
}