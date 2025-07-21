using System.Net;

namespace MaIN.Domain.Exceptions;

public class EmptyChatException(string chatId) : MaINCustomException($"Chat with id: '{chatId}' is empty. Complete operation is impossible.")
{
    public override string PublicErrorMessage => "Complete operation is impossible, because chat has no message.";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Conflict;
}