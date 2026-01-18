using System.Net;

namespace MaIN.Domain.Exceptions.Chats;

public class ChatNotInitializedException() : MaINCustomException("Chat has not been created yet. Call 'CompleteAsync' operation first.")
{
    public override string PublicErrorMessage => Message;
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Conflict;
}