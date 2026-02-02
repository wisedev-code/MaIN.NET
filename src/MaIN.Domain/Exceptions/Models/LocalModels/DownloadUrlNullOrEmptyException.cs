using System.Net;

namespace MaIN.Domain.Exceptions.Models.LocalModels;

public class DownloadUrlNullOrEmptyException()
    : MaINCustomException("Download url cannot be null or empty.")
{
    public override string PublicErrorMessage => "Download url cannot be null or empty.";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;
}