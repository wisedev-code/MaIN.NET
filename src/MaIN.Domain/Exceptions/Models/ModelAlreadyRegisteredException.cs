using System.Net;

namespace MaIN.Domain.Exceptions.Models;

public class ModelAlreadyRegisteredException(string? modelId)
    : MaINCustomException($"Model {modelId ?? string.Empty} is already registered.")
{
    public override string PublicErrorMessage => Message;
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;
}
