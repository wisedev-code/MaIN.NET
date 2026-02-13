using System.Net;

namespace MaIN.Domain.Exceptions.Models;

public class ModelNotRegisteredException(string? modelId)
    : MaINCustomException($"Model {modelId ?? string.Empty} is not registered. Consider calling ModelRegistry.GetAll() to get all registered models.")
{
    public override string PublicErrorMessage => Message;
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.NotFound;
}