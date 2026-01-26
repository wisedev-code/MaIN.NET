using System.Net;

namespace MaIN.Domain.Exceptions.Models;

public class ModelNotDownloadedException(string? modelName) 
    : MaINCustomException($"Given model {modelName ?? string.Empty} is not downloaded.")
{
    public override string PublicErrorMessage => Message;
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.NotFound;
}