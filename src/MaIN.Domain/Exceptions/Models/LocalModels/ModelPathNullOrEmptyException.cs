using System.Net;

namespace MaIN.Domain.Exceptions.Models.LocalModels;

public class ModelPathNullOrEmptyException()
    : MaINCustomException("Model path is null or empty.")
{
    public override string PublicErrorMessage => Message;
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;
}