using System.Net;

namespace MaIN.Domain.Exceptions;

public class ApiRequestFailedException(HttpStatusCode statusCode, string requestUrl, string httpMethod) 
    : MaINCustomException($"API request failed with status code: {statusCode}. Request url: {requestUrl}. Http method: {httpMethod}.")
{
    public override string PublicErrorMessage => "An error occurred while processing an external API request";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.InternalServerError;
}