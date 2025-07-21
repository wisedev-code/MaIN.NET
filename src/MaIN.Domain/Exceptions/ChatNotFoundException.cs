using System.Net;

namespace MaIN.Domain.Exceptions;

public class ChatNotFoundException(string chatId) 
    : MaINCustomException($"Chat with id: '{chatId}' not found.")
{
    public override string PublicErrorMessage => "Chat not found.";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.NotFound;
}