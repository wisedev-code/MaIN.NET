using System.Net;

namespace MaIN.Domain.Exceptions;

public class ChatAlreadyExistsException(string chatId) 
    : MaINCustomException($"Chat with id: '{chatId}' already exists.")
{
    public override string PublicErrorMessage => "Chat already exists.";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Conflict;
}