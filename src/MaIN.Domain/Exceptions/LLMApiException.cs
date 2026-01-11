using System.Net;

namespace MaIN.Domain.Exceptions;

public class LLMApiException(string llmApiName, HttpStatusCode llmApiHttpStatusCode, string? errorMessage) 
    : MaINCustomException($"{llmApiName} error. {errorMessage ?? string.Empty}")
{
    public override string PublicErrorMessage => Message;
    public override HttpStatusCode HttpStatusCode => llmApiHttpStatusCode;
}